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

public class KpiServiceTests
{
    private readonly TestWorld _world = new();

    private KpiService ServiceAt(PlantClock? clock = null) => new(
        _world.Repo,
        new ScopeResolver(),
        clock ?? TestWorld.Now,
        Options.Create(new ThresholdsOptions()),
        Options.Create(new MetricsOptions()));

    private Task<Dtos.KpiSummaryDto> SummaryAsync(
        UserContext? user = null, DateOnly? date = null, Guid? lineId = null, PlantClock? clock = null) =>
        ServiceAt(clock).GetSummaryAsync(user ?? UserContext.Manager, date ?? TestWorld.Today, lineId, null);

    [Fact]
    public async Task PlanAttainment_CountsOnlyHoursThatHaveStarted()
    {
        // Clock 10:30: hours 6..10 count (5 hours of 90 / 100), hours 11 and 12 are still ahead.
        for (var hour = 6; hour <= 12; hour++)
        {
            _world.AddHourly(TestWorld.Today, hour, _world.Line1, 100, hour <= 10 ? 90 : 0);
        }

        var summary = await SummaryAsync();

        Assert.Equal(90, summary.PlanAttainment.Value!.Value, 6);
        Assert.Equal(RagStatus.Amber, summary.PlanAttainment.Status);
        Assert.Equal(95, summary.PlanAttainment.Target);
    }

    [Theory]
    [InlineData(96, RagStatus.Green)]
    [InlineData(90, RagStatus.Amber)]
    [InlineData(80, RagStatus.Red)]
    public async Task PlanAttainment_StatusFollowsThresholds(int good, RagStatus expected)
    {
        _world.AddHourly(TestWorld.Today, 8, _world.Line1, 100, good);

        var summary = await SummaryAsync();

        Assert.Equal(expected, summary.PlanAttainment.Status);
    }

    [Fact]
    public async Task PlanAttainment_DeltaComparesWithTheSameHoursYesterday()
    {
        _world.AddHourly(TestWorld.Today, 8, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 80);
        // Yesterday's later hours must not count: today has not reached them yet.
        _world.AddHourly(TestWorld.Yesterday, 12, _world.Line1, 100, 100);

        var summary = await SummaryAsync();

        Assert.Equal(10, summary.PlanAttainment.DeltaVsPrevDay!.Value, 6);
    }

    [Fact]
    public async Task PlanAttainment_WithoutPlan_IsUnknown()
    {
        var summary = await SummaryAsync();

        Assert.Null(summary.PlanAttainment.Value);
        Assert.Equal(RagStatus.Unknown, summary.PlanAttainment.Status);
        Assert.Null(summary.PlanAttainment.DeltaVsPrevDay);
    }

    [Fact]
    public async Task LineRty_IsTheProductOfStationTypeYields()
    {
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Spi, 100, 98);
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Aoi, 100, 96);
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Ict, 100, 95);
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Fct, 100, 99);

        var summary = await SummaryAsync(TestWorld.Operator(_world.Line1));

        Assert.Equal(0.98 * 0.96 * 0.95 * 0.99 * 100, summary.LineRty.Value!.Value, 6);
        Assert.False(summary.LineRty.Incomplete);
        Assert.Equal(92, summary.LineRty.Target);
        Assert.Equal(RagStatus.Amber, summary.LineRty.Status);
    }

    [Fact]
    public async Task LineRty_WithMissingStationType_IsIncomplete()
    {
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Spi, 100, 98);

        var summary = await SummaryAsync(TestWorld.Operator(_world.Line1));

        Assert.Equal(98, summary.LineRty.Value!.Value, 6);
        Assert.True(summary.LineRty.Incomplete);
    }

    [Fact]
    public async Task LineRty_DeltaComparesWithYesterdaySameHours()
    {
        _world.AddTestsForAllTypes(TestWorld.Today, 8, _world.Line1, 100, 100);
        _world.AddTestsForAllTypes(TestWorld.Yesterday, 8, _world.Line1, 100, 100);
        // Yesterday after 10:30 must be ignored, otherwise the delta would not be zero.
        _world.AddTestsForAllTypes(TestWorld.Yesterday, 12, _world.Line1, 100, 50);

        var summary = await SummaryAsync(TestWorld.Operator(_world.Line1));

        Assert.Equal(0, summary.LineRty.DeltaVsPrevDay!.Value, 6);
    }

    [Fact]
    public async Task LineRty_WithoutData_IsUnknownAndIncomplete()
    {
        var summary = await SummaryAsync();

        Assert.Null(summary.LineRty.Value);
        Assert.Equal(RagStatus.Unknown, summary.LineRty.Status);
        Assert.True(summary.LineRty.Incomplete);
    }

    [Fact]
    public async Task Dpmo_AndTopDefect_ComeFromTheDaysDefects()
    {
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Spi, 1000, 990);
        _world.AddDefect(TestWorld.Today, 8, _world.Line1, StationType.Ict, "OPN", 7);
        _world.AddDefect(TestWorld.Today, 9, _world.Line1, StationType.Aoi, "TMB", 3);
        _world.Repo.DefectCodes.Add(new DefectCode("OPN", "Open circuit", "Electrical"));

        var summary = await SummaryAsync();

        // 10 / (1000 * 200) * 1,000,000
        Assert.Equal(50, summary.Dpmo.Value!.Value, 6);
        Assert.NotNull(summary.TopDefect);
        Assert.Equal(("OPN", "Open circuit", 7), (summary.TopDefect.Code, summary.TopDefect.Name, summary.TopDefect.Count));
    }

    [Fact]
    public async Task NoDefects_HaveNoTopDefect()
    {
        _world.AddTest(TestWorld.Today, 8, _world.Line1, StationType.Spi, 1000, 1000);

        var summary = await SummaryAsync();

        Assert.Null(summary.TopDefect);
        Assert.Equal(0, summary.Dpmo.Value);
    }

    [Fact]
    public async Task Today_IgnoresDefectsOfHoursThatHaveNotStarted()
    {
        _world.AddDefect(TestWorld.Today, 12, _world.Line1, StationType.Aoi, "TMB", 99);

        var summary = await SummaryAsync();

        Assert.Null(summary.TopDefect);
    }

    [Fact]
    public async Task PastDate_UsesTheFullDayAndComparesWithTheFullPreviousDay()
    {
        var dayBefore = TestWorld.Yesterday.AddDays(-1);
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 13, _world.Line1, 100, 90);
        _world.AddHourly(dayBefore, 8, _world.Line1, 100, 70);
        _world.AddHourly(dayBefore, 13, _world.Line1, 100, 70);

        var summary = await SummaryAsync(date: TestWorld.Yesterday);

        Assert.Equal(TestWorld.Yesterday, summary.Date);
        Assert.Equal(90, summary.PlanAttainment.Value!.Value, 6);
        Assert.Equal(20, summary.PlanAttainment.DeltaVsPrevDay!.Value, 6);
    }

    [Fact]
    public async Task FutureDate_HasNoValues()
    {
        var tomorrow = TestWorld.Today.AddDays(1);
        _world.AddHourly(tomorrow, 8, _world.Line1, 100, 90);

        var summary = await SummaryAsync(date: tomorrow);

        Assert.Null(summary.PlanAttainment.Value);
        Assert.Equal(RagStatus.Unknown, summary.PlanAttainment.Status);
        Assert.Null(summary.TopDefect);
    }

    [Fact]
    public async Task Operator_SeesOnlyOwnLine()
    {
        _world.AddHourly(TestWorld.Today, 8, _world.Line1, 100, 100);
        _world.AddHourly(TestWorld.Today, 8, _world.Line2, 100, 0);

        var own = await SummaryAsync(TestWorld.Operator(_world.Line1));
        var manager = await SummaryAsync();

        Assert.Equal(100, own.PlanAttainment.Value!.Value, 6);
        Assert.Equal(50, manager.PlanAttainment.Value!.Value, 6);
    }

    [Fact]
    public async Task LineFilter_LimitsManagerToThatLine()
    {
        _world.AddHourly(TestWorld.Today, 8, _world.Line1, 100, 100);
        _world.AddHourly(TestWorld.Today, 8, _world.Line2, 100, 0);

        var summary = await SummaryAsync(lineId: _world.Line2.Id);

        Assert.Equal(0, summary.PlanAttainment.Value);
    }

    [Fact]
    public async Task ForeignLine_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            SummaryAsync(TestWorld.Operator(_world.Line1), lineId: _world.Line2.Id));
    }
}
