using System.Security.Claims;
using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Core.Models.Enums;
using LossLens.Infrastructure.Security;

namespace LossLens.Api.Auth;

/// <summary>Builds the <see cref="UserContext"/> from the claims of the validated access token.</summary>
public sealed class JwtUserContextProvider(IHttpContextAccessor accessor) : IUserContextProvider
{
    public UserContext GetCurrent()
    {
        var principal = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("There is no active HTTP request.");

        return FromClaims(principal);
    }

    /// <exception cref="UnauthorizedException">The token has no valid role or a line id is malformed.</exception>
    public static UserContext FromClaims(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var role = ParseRole(principal.FindFirst(JwtTokenIssuer.RoleClaim)?.Value);
        if (role == UserRole.Manager)
        {
            return UserContext.Manager;
        }

        var lineIds = principal.FindAll(JwtTokenIssuer.LineIdClaim)
            .Select(claim => Guid.TryParse(claim.Value, out var id)
                ? id
                : throw new UnauthorizedException("The token has a malformed line id."))
            .Distinct()
            .ToArray();

        return new UserContext(role, lineIds);
    }

    private static UserRole ParseRole(string? value)
    {
        foreach (var role in Enum.GetValues<UserRole>())
        {
            if (string.Equals(role.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                return role;
            }
        }

        throw new UnauthorizedException("The token has no valid role.");
    }
}
