using LossLens.Application.Tests.Support;
using LossLens.Application.Time;

namespace LossLens.Application.Tests;

public class PlantClockTests
{
    private static readonly TimeZoneInfo UtcPlus2 =
        TimeZoneInfo.CreateCustomTimeZone("Plant", TimeSpan.FromHours(2), "Plant", "Plant");

    [Fact]
    public void Now_InUtcZone_EqualsUtcTime()
    {
        var clock = TestWorld.ClockAt(TestWorld.Today, 10, 30);

        Assert.Equal(new DateTime(2026, 11, 16, 10, 30, 0), clock.Now);
    }

    [Fact]
    public void Now_UsesPlantTimeZone()
    {
        var utc = new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.Zero);
        var clock = new PlantClock(new FixedTimeProvider(utc), UtcPlus2);

        Assert.Equal(11, clock.Now.Hour);
    }

    [Fact]
    public void Today_CanDifferFromUtcDate()
    {
        var utc = new DateTimeOffset(2026, 11, 16, 23, 30, 0, TimeSpan.Zero);
        var clock = new PlantClock(new FixedTimeProvider(utc), UtcPlus2);

        Assert.Equal(new DateOnly(2026, 11, 17), clock.Today);
    }
}
