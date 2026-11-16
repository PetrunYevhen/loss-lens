using System.Security.Claims;
using LossLens.Api.Auth;
using LossLens.Application.Exceptions;
using LossLens.Core.Models.Enums;
using LossLens.Infrastructure.Security;

namespace LossLens.Api.Tests;

public class JwtUserContextProviderTests
{
    private static readonly Guid LineA = Guid.NewGuid();
    private static readonly Guid LineB = Guid.NewGuid();

    private static ClaimsPrincipal Principal(string? role, params string[] lineIds)
    {
        var claims = new List<Claim>();
        if (role is not null)
        {
            claims.Add(new Claim(JwtTokenIssuer.RoleClaim, role));
        }

        claims.AddRange(lineIds.Select(id => new Claim(JwtTokenIssuer.LineIdClaim, id)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void Manager_IgnoresLineClaims()
    {
        var user = JwtUserContextProvider.FromClaims(Principal("manager", LineA.ToString()));

        Assert.True(user.IsManager);
        Assert.Empty(user.LineIds);
    }

    [Fact]
    public void Operator_GetsItsLine()
    {
        var user = JwtUserContextProvider.FromClaims(Principal("operator", LineA.ToString()));

        Assert.Equal(UserRole.Operator, user.Role);
        Assert.True(user.CanSee(LineA));
        Assert.False(user.CanSee(LineB));
    }

    [Fact]
    public void Supervisor_GetsEveryLine_WithoutDuplicates()
    {
        var user = JwtUserContextProvider.FromClaims(
            Principal("Supervisor", LineA.ToString(), LineB.ToString(), LineA.ToString()));

        Assert.Equal(new[] { LineA, LineB }, user.LineIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    public void MissingOrUnknownRole_IsUnauthorized(string? role)
    {
        Assert.Throws<UnauthorizedException>(() => JwtUserContextProvider.FromClaims(Principal(role)));
    }

    [Fact]
    public void MalformedLineId_IsUnauthorized()
    {
        Assert.Throws<UnauthorizedException>(() =>
            JwtUserContextProvider.FromClaims(Principal("operator", "not-a-guid")));
    }
}
