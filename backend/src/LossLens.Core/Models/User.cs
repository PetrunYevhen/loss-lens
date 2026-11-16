using LossLens.Core.Models.Enums;

namespace LossLens.Core.Models;

/// <summary>A person who can sign in. The role and lines decide which production data they may see.</summary>
public class User
{
    private Guid[] _lineIds;

    public Guid Id { get; private set; }

    /// <summary>Sign-in name, stored trimmed and in lower case.</summary>
    public string Username { get; private set; }

    /// <summary>Output of the password hasher. The password itself is never stored.</summary>
    public string PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    /// <summary>Lines the user may see. Empty for a manager, who sees all lines.</summary>
    public IReadOnlyCollection<Guid> LineIds => _lineIds;

    // For EF Core.
    private User()
    {
        Username = null!;
        PasswordHash = null!;
        _lineIds = [];
    }

    public User(string username, string passwordHash, UserRole role, IEnumerable<Guid> lineIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentNullException.ThrowIfNull(lineIds);
        if (!Enum.IsDefined(role))
            throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.");

        var lines = ValidateLines(role, lineIds);

        Id = Guid.NewGuid();
        Username = Normalize(username);
        PasswordHash = passwordHash;
        Role = role;
        _lineIds = lines;
    }

    /// <summary>
    /// Replaces the lines of the user, for example when a new line is added. The same rules apply as when the
    /// user is created. A manager always has no lines.
    /// </summary>
    public void AssignLines(IEnumerable<Guid> lineIds) => _lineIds = ValidateLines(Role, lineIds);

    private static Guid[] ValidateLines(UserRole role, IEnumerable<Guid> lineIds)
    {
        ArgumentNullException.ThrowIfNull(lineIds);

        var lines = lineIds.Distinct().ToArray();
        if (lines.Contains(Guid.Empty))
            throw new ArgumentException("Line id cannot be an empty GUID.", nameof(lineIds));

        switch (role)
        {
            case UserRole.Operator when lines.Length != 1:
                throw new ArgumentException("An operator belongs to exactly one line.", nameof(lineIds));
            case UserRole.Supervisor when lines.Length == 0:
                throw new ArgumentException("A supervisor needs at least one line.", nameof(lineIds));
        }

        return role == UserRole.Manager ? [] : lines;
    }

    /// <summary>The form in which user names are stored and looked up.</summary>
    public static string Normalize(string username) => username.Trim().ToLowerInvariant();
}
