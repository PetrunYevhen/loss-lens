using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Core.Abstractions;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Services;

/// <summary>
/// Builds the <see cref="UserContext"/> of a signed-in user from the database. The access token only says
/// who the user is, the role and the lines come from here, so a new line or a changed assignment works on the
/// next request and a removed user loses access at once.
/// </summary>
public class UserContextService(IUserRepository users)
{
    /// <exception cref="UnauthorizedException">The user of the token does not exist any more.</exception>
    public async Task<UserContext> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId, ct)
            ?? throw new UnauthorizedException("The user of this token no longer exists.");

        return user.Role == UserRole.Manager
            ? UserContext.Manager
            : new UserContext(user.Role, user.LineIds.ToArray());
    }
}
