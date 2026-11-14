namespace LossLens.Core.Models.Enums;

/// <summary>Demo role that limits which data a user can see.</summary>
public enum UserRole
{
    /// <summary>Own line and own shift, read-only.</summary>
    Operator,

    /// <summary>Assigned lines.</summary>
    Supervisor,

    /// <summary>All lines.</summary>
    Manager
}
