using LossLens.Core.Common;

namespace LossLens.Core.Models;

public class TestRecord
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int Hour { get; private set; }
    public Guid StationId { get; private set; }
    public int TestedUnits { get; private set; }
    public int FirstPassUnits { get; private set; }

    public TestRecord(DateOnly date, int hour, Guid stationId, int testedUnits, int firstPassUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(testedUnits);
        ArgumentOutOfRangeException.ThrowIfNegative(firstPassUnits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstPassUnits, testedUnits);

        Id = Guid.NewGuid();
        Date = date;
        Hour = Guard.HourOfDay(hour);
        StationId = Guard.NotEmpty(stationId);
        TestedUnits = testedUnits;
        FirstPassUnits = firstPassUnits;
    }

    /// <summary>Units that did not pass on the first attempt.</summary>
    public int FailedUnits => TestedUnits - FirstPassUnits;
}
