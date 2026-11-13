using LossLens.Core.Common;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(defectCode);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        Id = Guid.NewGuid();
        Date = date;
        Hour = Guard.HourOfDay(hour);
        StationId = Guard.NotEmpty(stationId);
        DefectCode = defectCode.Trim().ToUpperInvariant();
        Count = count;
    }
}
