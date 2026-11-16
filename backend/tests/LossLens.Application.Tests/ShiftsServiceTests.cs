using LossLens.Application.Services;
using LossLens.Application.Tests.Support;
using LossLens.Core.Models;

namespace LossLens.Application.Tests;

public class ShiftsServiceTests
{
    [Fact]
    public async Task ReturnsShiftsOrderedByStart_WithTimesAsHoursAndMinutes()
    {
        var world = new TestWorld();
        world.Repo.Shifts.Add(new Shift("C", new TimeOnly(22, 0), new TimeOnly(6, 0)));

        var shifts = await new ShiftsService(world.Repo).GetShiftsAsync();

        Assert.Equal(new[] { "Day", "Night", "C" }, shifts.Select(s => s.Name));
        Assert.Equal(("06:00", "14:00"), (shifts[0].Start, shifts[0].End));
        Assert.Equal(("22:00", "06:00"), (shifts[2].Start, shifts[2].End));
        Assert.Equal(world.DayShift.Id, shifts[0].Id);
    }

    [Fact]
    public async Task NoShifts_GiveEmptyList()
    {
        var shifts = await new ShiftsService(new Fakes.FakeMetricsRepository()).GetShiftsAsync();

        Assert.Empty(shifts);
    }
}
