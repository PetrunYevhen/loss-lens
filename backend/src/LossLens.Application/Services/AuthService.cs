using LossLens.Application.Dtos;
using LossLens.Application.Exceptions;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;

namespace LossLens.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher passwordHasher, ITokenIssuer tokenIssuer)
{
    private const string InvalidCredentials = "Invalid username or password.";

    /// <summary>Checks the credentials and issues an access token.</summary>
    /// <exception cref="UnauthorizedException">
    /// The user does not exist or the password is wrong. Both cases give the same message, so the answer
    /// does not reveal which user names exist.
    /// </exception>
    public async Task<LoginResultDto> LoginAsync(string? username, string? password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new UnauthorizedException(InvalidCredentials);

        var user = await users.FindByUsernameAsync(User.Normalize(username), ct);

        // Verify also runs for an unknown user (null hash), so both failures take about the same time.
        if (!passwordHasher.Verify(password, user?.PasswordHash) || user is null)
            throw new UnauthorizedException(InvalidCredentials);

        var token = tokenIssuer.Issue(user);

        return new LoginResultDto(token.Value, token.ExpiresAt, user.Role, user.LineIds.ToArray());
    }
}
