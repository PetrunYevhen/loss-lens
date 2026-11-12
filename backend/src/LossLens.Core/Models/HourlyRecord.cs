namespace LossLens.Core.Models;

public class HourlyRecord
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public int Hour { get; private set; }
    public Guid LineId { get; private set; }
    public Guid ShiftId { get; private set; }
    public int PlannedUnits { get; private set; }
    public int GoodUnits { get; private set; }

    public HourlyRecord(DateOnly date, int hour, Guid lineId, Guid shiftId, int plannedUnits, int goodUnits)
    {
        if (plannedUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(plannedUnits), "Planned units cannot be negative.");
        if (goodUnits < 0)
            throw new ArgumentOutOfRangeException(nameof(goodUnits), "Good units cannot be negative.");

        Id = Guid.NewGuid();
        Date = date;
        Hour = hour;
        LineId = lineId;
        ShiftId = shiftId;
        PlannedUnits = plannedUnits;
        GoodUnits = goodUnits;
    }
}
