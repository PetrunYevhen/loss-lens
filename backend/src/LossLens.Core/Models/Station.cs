using LossLens.Core.Common;
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
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown station type.");

        Id = Guid.NewGuid();
        LineId = Guard.NotEmpty(lineId);
        Name = name;
        Type = type;
    }
}
