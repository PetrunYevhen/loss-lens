using LossLens.Core.Common;

namespace LossLens.Core.Models;

public class Shift
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public TimeOnly StartTime { get; private set; }
    public TimeOnly EndTime { get; private set; }

    public Shift(string name, TimeOnly startTime, TimeOnly endTime)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (startTime == endTime)
            throw new ArgumentException("Shift start and end must differ.", nameof(endTime));

        Id = Guid.NewGuid();
        Name = name;
        StartTime = startTime;
        EndTime = endTime;
    }

    /// <summary>True when the shift crosses midnight, e.g. 22:00-06:00.</summary>
    public bool CrossesMidnight => StartTime > EndTime;

    /// <summary>Whether the clock hour (0-23, starting at hh:00) belongs to this shift.</summary>
    public bool Contains(int hour)
    {
        var time = new TimeOnly(Guard.HourOfDay(hour), 0);

        return CrossesMidnight
            ? time >= StartTime || time < EndTime
            : time >= StartTime && time < EndTime;
    }
}
