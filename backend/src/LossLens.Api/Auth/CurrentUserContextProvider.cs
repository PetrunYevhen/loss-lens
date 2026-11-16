using System.Security.Claims;
using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Application.Services;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LossLens.Api.Auth;

/// <summary>
/// Finds the user named by the <c>sub</c> claim of the validated access token and loads the role and lines
/// of that user from the database. Registered per request, so the lookup runs once for each request.
/// </summary>
public sealed class CurrentUserContextProvider(IHttpContextAccessor accessor, UserContextService contexts)
    : IUserContextProvider
{
    private UserContext? _current;

    public async Task<UserContext> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_current is not null)
        {
            return _current;
        }

        var principal = accessor.HttpContext?.User
            ?? throw new InvalidOperationException("There is no active HTTP request.");

        return _current = await contexts.GetAsync(UserIdOf(principal), ct);
    }

    /// <exception cref="UnauthorizedException">The token has no valid <c>sub</c> claim.</exception>
    public static Guid UserIdOf(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) && id != Guid.Empty
            ? id
            : throw new UnauthorizedException("The token has no valid subject.");
    }
}
