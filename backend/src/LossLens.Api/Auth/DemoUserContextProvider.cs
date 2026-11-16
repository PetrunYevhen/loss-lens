using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Core.Models.Enums;

namespace LossLens.Api.Auth;

/// <summary>
/// Demo authentication: the role and lines come from request headers. Anyone can send any value, so this
/// is not secure. Replace it with a JWT based provider; the services only see <see cref="UserContext"/>.
/// </summary>
/// <remarks>
/// <c>X-Demo-Role</c>: operator, supervisor or manager (missing means manager).
/// <c>X-Demo-LineIds</c>: comma separated line ids. One for an operator, at least one for a supervisor,
/// ignored for a manager.
/// </remarks>
public sealed class DemoUserContextProvider(IHttpContextAccessor accessor) : IUserContextProvider
{
    public const string RoleHeader = "X-Demo-Role";
    public const string LineIdsHeader = "X-Demo-LineIds";

    public UserContext GetCurrent()
    {
        var request = accessor.HttpContext?.Request
            ?? throw new InvalidOperationException("There is no active HTTP request.");

        return Parse(request.Headers[RoleHeader].ToString(), request.Headers[LineIdsHeader].ToString());
    }

    /// <exception cref="ValidationException">The role is unknown, a line id is malformed or the line count does not fit the role.</exception>
    public static UserContext Parse(string? role, string? lineIds)
    {
        var parsedRole = ParseRole(role);
        if (parsedRole == UserRole.Manager)
        {
            return UserContext.Manager;
        }

        var ids = ParseLineIds(lineIds);
        if (parsedRole == UserRole.Operator && ids.Length != 1)
            throw new ValidationException($"{RoleHeader}: operator needs exactly one line in {LineIdsHeader}.");

        if (parsedRole == UserRole.Supervisor && ids.Length == 0)
            throw new ValidationException($"{RoleHeader}: supervisor needs at least one line in {LineIdsHeader}.");

        return new UserContext(parsedRole, ids);
    }

    private static UserRole ParseRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return UserRole.Manager;
        }

        foreach (var known in Enum.GetValues<UserRole>())
        {
            if (string.Equals(known.ToString(), role.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        throw new ValidationException($"{RoleHeader}: unknown role '{role}'. Use operator, supervisor or manager.");
    }

    private static Guid[] ParseLineIds(string? lineIds)
    {
        if (string.IsNullOrWhiteSpace(lineIds))
        {
            return [];
        }

        return lineIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(text => Guid.TryParse(text, out var id)
                ? id
                : throw new ValidationException($"{LineIdsHeader}: '{text}' is not a valid line id."))
            .Distinct()
            .ToArray();
    }
}
