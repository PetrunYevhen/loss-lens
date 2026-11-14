using LossLens.Core.Common;

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
        ArgumentOutOfRangeException.ThrowIfNegative(plannedUnits);
        ArgumentOutOfRangeException.ThrowIfNegative(goodUnits);

        Id = Guid.NewGuid();
        Date = date;
        Hour = Guard.HourOfDay(hour);
        LineId = Guard.NotEmpty(lineId);
        ShiftId = Guard.NotEmpty(shiftId);
        PlannedUnits = plannedUnits;
        GoodUnits = goodUnits;
    }
}
