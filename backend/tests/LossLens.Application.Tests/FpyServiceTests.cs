using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Application.Services;
using LossLens.Application.Tests.Support;
using LossLens.Application.Time;
using LossLens.Core.Config;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Tests;

public class FpyServiceTests
{
    private readonly TestWorld _world = new();

    private FpyService ServiceAt(PlantClock? clock = null) =>
        new(_world.Repo, new ScopeResolver(), Options.Create(new ThresholdsOptions()), clock ?? TestWorld.Now);

    // ---- GetStationsAsync -------------------------------------------------------------------

    [Fact]
    public async Task Stations_SingleLine_ReturnsEachStationWithStatus()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 100, 98);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, 100, 95);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Ict, 100, 90);

        var result = await ServiceAt().GetStationsAsync(TestWorld.Operator(_world.Line1), TestWorld.Yesterday, null, null);

        Assert.Equal(new[] { "SPI-1", "AOI-1", "ICT-1", "FCT-1" }, result.Select(s => s.Name));
        Assert.Equal(
            new[] { RagStatus.Green, RagStatus.Amber, RagStatus.Red, RagStatus.Unknown },
            result.Select(s => s.Status));
        Assert.Equal(98, result[0].Fpy);
        Assert.Null(result[3].Fpy);
        Assert.Equal(_world.Station(_world.Line1, StationType.Spi).Id.ToString(), result[0].StationId);
    }

    [Fact]
    public async Task Stations_ManagerWithLineFilter_ReturnsThatLinesStations()
    {
        var result = await ServiceAt().GetStationsAsync(UserContext.Manager, TestWorld.Yesterday, _world.Line2.Id, null);

        Assert.Equal(4, result.Count);
        Assert.Equal("SPI-2", result[0].Name);
    }

    [Fact]
    public async Task Stations_SeveralLines_AreCombinedByType()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 100, 98);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line2, StationType.Spi, 100, 96);

        var result = await ServiceAt().GetStationsAsync(UserContext.Manager, TestWorld.Yesterday, null, null);

        Assert.Equal(new[] { "SPI", "AOI", "ICT", "FCT" }, result.Select(s => s.StationId));
        var spi = result[0];
        Assert.Equal((200, 194), (spi.Tested, spi.FirstPass));
        Assert.Equal(97, spi.Fpy);
        Assert.Equal(RagStatus.Green, spi.Status);
    }

    [Fact]
    public async Task Stations_Combined_UsesSumsNotAveragedPercentages()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 10, 10);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line2, StationType.Spi, 990, 900);

        var result = await ServiceAt().GetStationsAsync(UserContext.Manager, TestWorld.Yesterday, null, null);

        // 910 / 1000 = 91.0, not the average of 100 and 90.9.
        Assert.Equal(91, result[0].Fpy);
    }

    [Fact]
    public async Task Stations_ShiftFilter_DropsHoursOutsideTheShift()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 100, 100);
        _world.AddTest(TestWorld.Yesterday, 15, _world.Line1, StationType.Spi, 100, 50);

        var result = await ServiceAt().GetStationsAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Yesterday, null, _world.DayShift.Id);

        Assert.Equal((100, 100), (result[0].Tested, result[0].FirstPass));
    }

    [Fact]
    public async Task Stations_ForeignLine_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            ServiceAt().GetStationsAsync(TestWorld.Operator(_world.Line1), TestWorld.Yesterday, _world.Line2.Id, null));
    }

    [Fact]
    public async Task Stations_OperatorWithoutLines_GetsNothing()
    {
        var result = await ServiceAt().GetStationsAsync(
            new UserContext(UserRole.Operator, []), TestWorld.Yesterday, null, null);

        Assert.Empty(result);
    }

    // ---- GetTrendAsync ----------------------------------------------------------------------

    private void AddFullDay(DateOnly date, ProductionLine line, int spi, int aoi, int ict, int fct)
    {
        _world.AddTest(date, 8, line, StationType.Spi, 100, spi);
        _world.AddTest(date, 8, line, StationType.Aoi, 100, aoi);
        _world.AddTest(date, 8, line, StationType.Ict, 100, ict);
        _world.AddTest(date, 8, line, StationType.Fct, 100, fct);
    }

    [Fact]
    public async Task Trend_Rty_IsTheProductOfStationFpys()
    {
        AddFullDay(TestWorld.Yesterday, _world.Line1, 98, 96, 95, 99);

        var points = await ServiceAt().GetTrendAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Yesterday, TestWorld.Yesterday, null);

        var expected = 0.98 * 0.96 * 0.95 * 0.99 * 100;
        Assert.Equal(expected, Assert.Single(points).Rty!.Value, 6);
    }

    [Fact]
    public async Task Trend_ReturnsEveryDayEvenWithoutData()
    {
        AddFullDay(new DateOnly(2026, 11, 12), _world.Line1, 100, 100, 100, 100);
        AddFullDay(new DateOnly(2026, 11, 14), _world.Line1, 90, 100, 100, 100);

        var points = await ServiceAt().GetTrendAsync(
            UserContext.Manager, new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 14), null);

        Assert.Equal(3, points.Count);
        Assert.Equal(100, points[0].Rty);
        Assert.Null(points[1].Rty);
        Assert.Equal(90, points[2].Rty!.Value, 6);
        Assert.Equal(
            new[] { new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 13), new DateOnly(2026, 11, 14) },
            points.Select(p => p.Date));
    }

    [Fact]
    public async Task Trend_RangeEndingInTheFuture_IsCutAtToday()
    {
        var points = await ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Today.AddDays(-2), TestWorld.Today.AddDays(5), null);

        Assert.Equal(TestWorld.Today, points[^1].Date);
        Assert.Equal(3, points.Count);
    }

    [Fact]
    public async Task Trend_RangeEntirelyInTheFuture_IsEmpty()
    {
        var points = await ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Today.AddDays(1), TestWorld.Today.AddDays(3), null);

        Assert.Empty(points);
    }

    [Fact]
    public async Task Trend_SeveralLines_AreCombinedByType()
    {
        AddFullDay(TestWorld.Yesterday, _world.Line1, 100, 100, 100, 100);
        AddFullDay(TestWorld.Yesterday, _world.Line2, 90, 100, 100, 100);

        var points = await ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Yesterday, TestWorld.Yesterday, null);

        // SPI: 190 / 200 = 95%, the other types are 100%.
        Assert.Equal(95, Assert.Single(points).Rty!.Value, 6);
    }

    [Fact]
    public async Task Trend_MissingStationType_IsSkipped()
    {
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Spi, 100, 90);
        _world.AddTest(TestWorld.Yesterday, 8, _world.Line1, StationType.Aoi, 100, 80);

        var points = await ServiceAt().GetTrendAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Yesterday, TestWorld.Yesterday, null);

        Assert.Equal(72, Assert.Single(points).Rty!.Value, 6);
    }

    [Fact]
    public async Task Trend_OperatorDoesNotSeeOtherLines()
    {
        AddFullDay(TestWorld.Yesterday, _world.Line1, 100, 100, 100, 100);
        AddFullDay(TestWorld.Yesterday, _world.Line2, 50, 100, 100, 100);

        var points = await ServiceAt().GetTrendAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Yesterday, TestWorld.Yesterday, null);

        Assert.Equal(100, Assert.Single(points).Rty);
    }

    [Fact]
    public async Task Trend_ForeignLine_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceAt().GetTrendAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Yesterday, TestWorld.Yesterday, _world.Line2.Id));
    }

    [Fact]
    public async Task Trend_ReversedRange_IsInvalid()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            ServiceAt().GetTrendAsync(UserContext.Manager, TestWorld.Today, TestWorld.Yesterday, null));
    }

    [Fact]
    public async Task Trend_RangeLongerThanLimit_IsInvalid()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            ServiceAt().GetTrendAsync(UserContext.Manager, TestWorld.Today.AddDays(-92), TestWorld.Today, null));
    }

    [Fact]
    public async Task Trend_RangeOfExactlyTheLimit_IsAccepted()
    {
        var points = await ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Today.AddDays(-91), TestWorld.Today, null);

        Assert.Equal(92, points.Count);
    }

    [Fact]
    public async Task Stations_UnknownLineOrShift_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            ServiceAt().GetStationsAsync(UserContext.Manager, TestWorld.Yesterday, Guid.NewGuid(), null));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            ServiceAt().GetStationsAsync(UserContext.Manager, TestWorld.Yesterday, null, Guid.NewGuid()));
    }

    [Fact]
    public async Task Trend_UnknownLine_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Yesterday, TestWorld.Yesterday, Guid.NewGuid()));
    }

    [Fact]
    public async Task Trend_ForeignLine_IsForbidden_EvenForARangeInTheFuture()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => ServiceAt().GetTrendAsync(
            TestWorld.Operator(_world.Line1), TestWorld.Today.AddDays(5), TestWorld.Today.AddDays(6), _world.Line2.Id));
    }

    [Fact]
    public async Task Trend_UnknownLine_IsNotFound_EvenForARangeInTheFuture()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => ServiceAt().GetTrendAsync(
            UserContext.Manager, TestWorld.Today.AddDays(5), TestWorld.Today.AddDays(6), Guid.NewGuid()));
    }
}
