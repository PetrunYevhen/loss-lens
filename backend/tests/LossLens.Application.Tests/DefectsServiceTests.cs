using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Application.Services;
using LossLens.Application.Tests.Support;
using LossLens.Core.Config;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Tests;

public class DefectsServiceTests
{
    private readonly TestWorld _world = new();

    private DefectsService Service(int opportunitiesPerUnit = 200) =>
        new(_world.Repo, new ScopeResolver(), Options.Create(new MetricsOptions { OpportunitiesPerUnit = opportunitiesPerUnit }));

    private Task<Dtos.ParetoResponseDto> GetAsync(
        UserContext user, Guid? lineId = null, Guid? stationId = null, Guid? shiftId = null) =>
        Service().GetParetoAsync(user, TestWorld.Yesterday, TestWorld.Yesterday, lineId, stationId, shiftId);

    [Fact]
    public async Task Pareto_IsSortedByCountWithSharesAndCumulativeShare()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, "OPN", 6);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 3);
        _world.AddDefect(TestWorld.Yesterday, 9, _world.Line1, StationType.Aoi, "MIS-CMP", 1);

        var result = await GetAsync(UserContext.Manager);

        Assert.Equal(new[] { "OPN", "TMB", "MIS-CMP" }, result.Items.Select(i => i.Code));
        Assert.Equal(new[] { 6, 3, 1 }, result.Items.Select(i => i.Count));
        Assert.Equal(new[] { 60.0, 30.0, 10.0 }, result.Items.Select(i => i.Share));
        Assert.Equal(new[] { 60.0, 90.0, 100.0 }, result.Items.Select(i => i.CumulativeShare));
    }

    [Fact]
    public async Task Pareto_SumsTheSameCodeAcrossStationsAndHours()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 2);
        _world.AddDefect(TestWorld.Yesterday, 9, _world.Line1, StationType.Aoi, "TMB", 3);
        _world.AddDefect(TestWorld.Yesterday, 9, _world.Line2, StationType.Ict, "TMB", 4);

        var result = await GetAsync(UserContext.Manager);

        Assert.Equal(9, Assert.Single(result.Items).Count);
    }

    [Fact]
    public async Task Pareto_UsesCodeNames_AndFallsBackToTheCode()
    {
        _world.Repo.DefectCodes.Add(new DefectCode("OPN", "Open circuit", "Electrical"));
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, "OPN", 2);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, "XYZ", 1);

        var result = await GetAsync(UserContext.Manager);

        Assert.Equal(new[] { "Open circuit", "XYZ" }, result.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task Dpmo_DividesDefectsByUnitsEnteringTheLine()
    {
        // 1000 units enter at SPI. Later stations see fewer boards and must not change the unit count.
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 1000, 990);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, 900, 880);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 10);

        var result = await GetAsync(UserContext.Manager);

        // 10 / (1000 * 200) * 1,000,000
        Assert.Equal(50, result.Dpmo!.Value, 6);
    }

    [Fact]
    public async Task Dpmo_SumsUnitsOfEveryLineInScope()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 600, 600);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line2, StationType.Spi, 400, 400);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 10);

        var result = await GetAsync(UserContext.Manager);

        Assert.Equal(50, result.Dpmo!.Value, 6);
    }

    [Fact]
    public async Task Dpmo_UsesConfiguredOpportunitiesPerUnit()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 1000, 1000);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 10);

        var result = await Service(opportunitiesPerUnit: 100)
            .GetParetoAsync(UserContext.Manager, TestWorld.Yesterday, TestWorld.Yesterday, null, null, null);

        Assert.Equal(100, result.Dpmo!.Value, 6);
    }

    [Fact]
    public async Task Dpmo_IsNullWhenNothingWasTested()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 10);

        var result = await GetAsync(UserContext.Manager);

        Assert.Null(result.Dpmo);
    }

    [Fact]
    public async Task NoDefects_GiveEmptyItemsAndZeroDpmo()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 1000, 1000);

        var result = await GetAsync(UserContext.Manager);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.Dpmo);
    }

    [Fact]
    public async Task StationFilter_LimitsDefectsAndUnitsToThatStation()
    {
        var ict = _world.Station(_world.Line1, StationType.Ict);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 1000, 1000);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, 500, 490);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, "OPN", 10);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 50);

        var result = await GetAsync(UserContext.Manager, stationId: ict.Id);

        Assert.Equal("OPN", Assert.Single(result.Items).Code);
        // 10 / (500 * 200) * 1,000,000
        Assert.Equal(100, result.Dpmo!.Value, 6);
    }

    [Fact]
    public async Task ShiftFilter_DropsDefectsOutsideTheShiftHours()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 4);
        _world.AddDefect(TestWorld.Yesterday, 15, _world.Line1, StationType.Aoi, "OPN", 9);

        var result = await GetAsync(UserContext.Manager, shiftId: _world.DayShift.Id);

        Assert.Equal("TMB", Assert.Single(result.Items).Code);
    }

    [Fact]
    public async Task Operator_SeesOnlyDefectsOfOwnLine()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 4);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line2, StationType.Aoi, "OPN", 9);

        var result = await GetAsync(TestWorld.Operator(_world.Line1));

        Assert.Equal("TMB", Assert.Single(result.Items).Code);
    }

    [Fact]
    public async Task StationOfAnotherLine_GivesNoDataForOperator()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line2, StationType.Aoi, "OPN", 9);
        var foreign = _world.Station(_world.Line2, StationType.Aoi);

        var result = await GetAsync(TestWorld.Operator(_world.Line1), stationId: foreign.Id);

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task ForeignLine_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            GetAsync(TestWorld.Operator(_world.Line1), lineId: _world.Line2.Id));
    }

    [Fact]
    public async Task DateRange_CoversAllDaysInclusive()
    {
        _world.AddDefect(TestWorld.Yesterday.AddDays(-1), 8, _world.Line1, StationType.Aoi, "TMB", 1);
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, "TMB", 2);
        _world.AddDefect(TestWorld.Today, 8, _world.Line1, StationType.Aoi, "TMB", 4);

        var result = await Service().GetParetoAsync(
            UserContext.Manager, TestWorld.Yesterday.AddDays(-1), TestWorld.Yesterday, null, null, null);

        Assert.Equal(3, Assert.Single(result.Items).Count);
    }

    [Fact]
    public async Task ReversedRange_IsInvalid()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Service().GetParetoAsync(UserContext.Manager, TestWorld.Today, TestWorld.Yesterday, null, null, null));
    }

    [Fact]
    public async Task RangeLongerThanLimit_IsInvalid()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            Service().GetParetoAsync(UserContext.Manager, TestWorld.Today.AddDays(-92), TestWorld.Today, null, null, null));
    }

    [Fact]
    public async Task UnknownLine_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(UserContext.Manager, lineId: Guid.NewGuid()));
    }

    [Fact]
    public async Task UnknownStation_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(UserContext.Manager, stationId: Guid.NewGuid()));
    }

    [Fact]
    public async Task UnknownShift_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(UserContext.Manager, shiftId: Guid.NewGuid()));
    }

    [Fact]
    public async Task StationOfAnotherLine_IsNotAnError_ButGivesNothing()
    {
        _world.AddDefect(TestWorld.Yesterday, 8, _world.Line2, StationType.Aoi, "TMB", 3);

        var result = await GetAsync(
            TestWorld.Operator(_world.Line1), stationId: _world.Station(_world.Line2, StationType.Aoi).Id);

        Assert.Empty(result.Items);
    }
}
