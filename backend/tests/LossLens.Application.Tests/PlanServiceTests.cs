using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Application.Services;
using LossLens.Application.Tests.Support;
using LossLens.Application.Time;

namespace LossLens.Application.Tests;

public class PlanServiceTests
{
    private readonly TestWorld _world = new();

    private PlanService ServiceAt(PlantClock clock) => new(_world.Repo, new ScopeResolver(), clock);

    private Task<IReadOnlyList<Dtos.PlanVsActualPointDto>> GetAsync(
        PlanService service, UserContext user, DateOnly date, Guid? lineId = null, Guid? shiftId = null) =>
        service.GetAsync(user, date, lineId, shiftId);

    [Fact]
    public async Task PastDay_HasActualForEveryHour()
    {
        _world.AddHourly(TestWorld.Yesterday, 6, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 13, _world.Line1, 100, 80);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday);

        Assert.Equal(new int?[] { 90, 80 }, points.Select(p => p.Good));
    }

    [Fact]
    public async Task FutureDay_HasNoActual()
    {
        var tomorrow = TestWorld.Today.AddDays(1);
        _world.AddHourly(tomorrow, 6, _world.Line1, 100, 55);
        _world.AddHourly(tomorrow, 7, _world.Line1, 100, 55);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, tomorrow);

        Assert.Equal(2, points.Count);
        Assert.All(points, p => Assert.Null(p.Good));
        Assert.Equal(new[] { 100, 100 }, points.Select(p => p.Planned));
    }

    [Fact]
    public async Task Today_HidesActualOfHoursThatHaveNotStarted()
    {
        for (var hour = 9; hour <= 12; hour++)
        {
            _world.AddHourly(TestWorld.Today, hour, _world.Line1, 100, 70);
        }

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Today);

        // The clock is 10:30: hour 10 is running and counts, hours 11 and 12 are still ahead.
        Assert.Equal(new int?[] { 70, 70, null, null }, points.Select(p => p.Good));
    }

    [Fact]
    public async Task Today_IgnoresStoredActualOfFutureHours()
    {
        _world.AddHourly(TestWorld.Today, 12, _world.Line1, 100, 999);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Today);

        Assert.Null(Assert.Single(points).Good);
    }

    [Fact]
    public async Task HourStartingExactlyNow_HasStarted()
    {
        _world.AddHourly(TestWorld.Today, 10, _world.Line1, 100, 40);

        var points = await GetAsync(ServiceAt(TestWorld.ClockAt(TestWorld.Today, 10, 0)), UserContext.Manager, TestWorld.Today);

        Assert.Equal(40, Assert.Single(points).Good);
    }

    [Fact]
    public async Task Manager_SumsLinesPerHour()
    {
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line2, 80, 60);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday);

        var point = Assert.Single(points);
        Assert.Equal((8, 180, 150), (point.Hour, point.Planned, point.Good));
    }

    [Fact]
    public async Task Points_AreOrderedByHour()
    {
        _world.AddHourly(TestWorld.Yesterday, 12, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 7, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 9, _world.Line1, 100, 90);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday);

        Assert.Equal(new[] { 7, 9, 12 }, points.Select(p => p.Hour));
    }

    [Fact]
    public async Task Operator_SeesOnlyOwnLine()
    {
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line2, 80, 60);

        var points = await GetAsync(ServiceAt(TestWorld.Now), TestWorld.Operator(_world.Line2), TestWorld.Yesterday);

        Assert.Equal(80, Assert.Single(points).Planned);
    }

    [Fact]
    public async Task ForeignLine_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            GetAsync(ServiceAt(TestWorld.Now), TestWorld.Operator(_world.Line1), TestWorld.Yesterday, _world.Line2.Id));
    }

    [Fact]
    public async Task LineFilter_LimitsManagerToThatLine()
    {
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 90);
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line2, 80, 60);

        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday, _world.Line1.Id);

        Assert.Equal(100, Assert.Single(points).Planned);
    }

    [Fact]
    public async Task ShiftFilter_KeepsOnlyThatShift()
    {
        _world.AddHourly(TestWorld.Yesterday, 8, _world.Line1, 100, 90, _world.DayShift);
        _world.AddHourly(TestWorld.Yesterday, 15, _world.Line1, 100, 70, _world.NightShift);

        var points = await GetAsync(
            ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday, shiftId: _world.NightShift.Id);

        Assert.Equal(15, Assert.Single(points).Hour);
    }

    [Fact]
    public async Task DayWithoutRecords_GivesEmptyList()
    {
        var points = await GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday);

        Assert.Empty(points);
    }

    [Fact]
    public async Task UnknownLine_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday, lineId: Guid.NewGuid()));
    }

    [Fact]
    public async Task UnknownShift_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            GetAsync(ServiceAt(TestWorld.Now), UserContext.Manager, TestWorld.Yesterday, shiftId: Guid.NewGuid()));
    }
}
