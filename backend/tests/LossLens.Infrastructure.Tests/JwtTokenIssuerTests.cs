using System.Text;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using LossLens.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LossLens.Infrastructure.Tests;

public class JwtTokenIssuerTests
{
    private const string Key = "unit-test-signing-key-of-at-least-32-bytes!";
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 10, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Issuer = "test-issuer",
        Audience = "test-audience",
        Key = Key,
        LifetimeMinutes = 30
    };

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static JwtTokenIssuer Issuer(DateTimeOffset? now = null) =>
        new(Microsoft.Extensions.Options.Options.Create(Options), new FixedTime(now ?? Now));

    private static async Task<TokenValidationResult> ValidateAsync(string token, string key = Key) =>
        await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = false,
            NameClaimType = "name",
            RoleClaimType = "role"
        });

    [Fact]
    public async Task Token_CarriesSubjectNameRoleAndEveryLine()
    {
        var lineA = Guid.NewGuid();
        var lineB = Guid.NewGuid();
        var user = new User("Supervisor", "hash", UserRole.Supervisor, [lineA, lineB]);

        var token = Issuer().Issue(user);
        var result = await ValidateAsync(token.Value);

        Assert.True(result.IsValid);
        var jwt = (JsonWebToken)result.SecurityToken;
        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal("supervisor", jwt.GetClaim("name").Value);
        Assert.Equal("supervisor", jwt.GetClaim("role").Value);
        Assert.Equivalent(
            new[] { lineA.ToString(), lineB.ToString() },
            jwt.Claims.Where(c => c.Type == JwtTokenIssuer.LineIdClaim).Select(c => c.Value).ToArray());
    }

    [Fact]
    public void Manager_HasNoLineClaims()
    {
        var token = Issuer().Issue(new User("boss", "hash", UserRole.Manager, []));

        var jwt = new JsonWebToken(token.Value);

        Assert.Equal("manager", jwt.GetClaim("role").Value);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == JwtTokenIssuer.LineIdClaim);
    }

    [Fact]
    public void ExpiresAt_IsNowPlusTheLifetime_AndMatchesTheToken()
    {
        var token = Issuer().Issue(new User("boss", "hash", UserRole.Manager, []));

        Assert.Equal(Now.AddMinutes(30), token.ExpiresAt);
        Assert.Equal(token.ExpiresAt.UtcDateTime, new JsonWebToken(token.Value).ValidTo, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Token_SignedWithAnotherKey_IsRejected()
    {
        var token = Issuer().Issue(new User("boss", "hash", UserRole.Manager, []));

        var result = await ValidateAsync(token.Value, key: "a-different-signing-key-of-32-bytes-or-more!");

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ExpiredToken_IsRejectedWhenLifetimeIsChecked()
    {
        // Issued two hours ago with a 30 minute lifetime, checked against the real clock.
        var token = Issuer(now: DateTimeOffset.UtcNow.AddHours(-2)).Issue(new User("boss", "hash", UserRole.Manager, []));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        });

        Assert.False(result.IsValid);
    }
}
