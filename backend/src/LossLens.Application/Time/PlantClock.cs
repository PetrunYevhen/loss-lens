namespace LossLens.Application.Time;

/// <summary>
/// Current time in the plant's time zone. Shift hours are plant-local, so "now" must not be UTC.
/// Wraps <see cref="TimeProvider"/> so tests can control the time.
/// </summary>
public sealed class PlantClock(TimeProvider timeProvider, TimeZoneInfo timeZone)
{
    /// <summary>Plant-local date and time.</summary>
    public DateTime Now => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;

    /// <summary>Plant-local date.</summary>
    public DateOnly Today => DateOnly.FromDateTime(Now);
}
