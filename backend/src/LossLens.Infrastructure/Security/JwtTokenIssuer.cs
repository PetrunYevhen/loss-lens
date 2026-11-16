using System.Security.Claims;
using System.Text;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LossLens.Infrastructure.Security;

/// <summary>
/// Issues signed JWT access tokens. Claims: <c>sub</c> (user id), <c>name</c>, <c>role</c> (manager, supervisor
/// or operator) and one <c>line_id</c> per allowed line.
/// </summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    public const string RoleClaim = "role";
    public const string LineIdClaim = "line_id";

    public AccessToken Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(settings.LifetimeMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(RoleClaim, user.Role.ToString().ToLowerInvariant())
        };
        claims.AddRange(user.LineIds.Select(id => new Claim(LineIdClaim, id.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                SecurityAlgorithms.HmacSha256)
        };

        return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}
