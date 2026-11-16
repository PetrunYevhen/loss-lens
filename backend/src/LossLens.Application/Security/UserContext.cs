using LossLens.Core.Models.Enums;

namespace LossLens.Application.Security;

public sealed class UserContext
{
    public UserRole Role { get; }
    public IReadOnlyCollection<Guid> LineIds { get; }

    public UserContext(UserRole role, IReadOnlyCollection<Guid> lineIds)
    {
        ArgumentNullException.ThrowIfNull(lineIds);
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.");
        
        Role = role;
        LineIds = lineIds;
    }
    
    public static UserContext Manager { get; } = new(UserRole.Manager, []);
    
    public bool IsManager => Role == UserRole.Manager;
    public bool CanSee(Guid lineId) => IsManager || LineIds.Contains(lineId);
}