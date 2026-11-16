using LossLens.Application.Exceptions;

namespace LossLens.Application.Security;

/// <summary>
/// Turns the optional <c>lineId</c> request filter plus the caller's role into the set of lines a
/// service may read. Keeps the scope rule (AC-7) in one place.
/// </summary>
public sealed class ScopeResolver
{
    /// <summary>
    /// Returns the line ids to query. <c>null</c> means "all lines" (a manager without a filter),
    /// matching the repository convention. An empty collection means "no lines".
    /// </summary>
    /// <exception cref="ForbiddenException">The user asked for a line outside their scope.</exception>
    public IReadOnlyCollection<Guid>? Resolve(UserContext user, Guid? lineId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (lineId is { } requested)
        {
            return user.CanSee(requested)
                ? [requested]
                : throw new ForbiddenException($"Line {requested} is outside your scope.");
        }

        return user.IsManager ? null : user.LineIds;
    }
}
