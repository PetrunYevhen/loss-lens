using LossLens.Core.Models.Enums;

namespace LossLens.Core.Models;

public class Station
{
    public Guid Id { get; private set; }
    public Guid LineId { get; private set; }
    public string Name { get; private set; }
    public StationType Type { get; private set; }

    public Station(Guid lineId, string name, StationType type)
    {
        Id = Guid.NewGuid();
        LineId = lineId;
        Name = name;
        Type = type;
    }
}
