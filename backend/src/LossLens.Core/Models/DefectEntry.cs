namespace LossLens.Core.Models;

public class DefectEntry
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int Hour { get; private set; }
    public Guid StationId { get; private set; }
    public string DefectCode { get; private set; }
    public int Count { get; private set; }

    public DefectEntry(DateOnly date, int hour, Guid stationId, string defectCode, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Defect count cannot be negative.");

        Id = Guid.NewGuid();
        Date = date;
        Hour = hour;
        StationId = stationId;
        DefectCode = defectCode;
        Count = count;
    }
}
