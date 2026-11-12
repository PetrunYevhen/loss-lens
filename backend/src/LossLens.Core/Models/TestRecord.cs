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
        if (testedUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(testedUnits), "Tested units cannot be negative.");
        if (firstPassUnits < 0 || firstPassUnits > testedUnits)
            throw new ArgumentOutOfRangeException(nameof(firstPassUnits), "First pass units must be between 0 and tested units.");

        Id = Guid.NewGuid();
        Date = date;
        Hour = hour;
        StationId = stationId;
        TestedUnits = testedUnits;
        FirstPassUnits = firstPassUnits;
    }
}
