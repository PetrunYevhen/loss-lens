using System.Security.Claims;
using LossLens.Api.Auth;
using LossLens.Application.Exceptions;
using LossLens.Application.Services;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace LossLens.Api.Tests;

public class CurrentUserContextProviderTests
{
    private static ClaimsPrincipal Principal(string? subject)
    {
        var claims = new List<Claim>();
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void UserIdOf_ReadsTheSubjectClaim()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id, CurrentUserContextProvider.UserIdOf(Principal(id.ToString())));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void MissingOrMalformedSubject_IsUnauthorized(string? subject)
    {
        Assert.Throws<UnauthorizedException>(() => CurrentUserContextProvider.UserIdOf(Principal(subject)));
    }

    [Fact]
    public async Task GetCurrent_LoadsTheLinesFromTheDatabase_OnceForARequest()
    {
        var line = Guid.NewGuid();
        var user = new User("op", "hash", UserRole.Operator, [line]);
        var users = new CountingUsers(user);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(user.Id.ToString()) } };
        var provider = new CurrentUserContextProvider(accessor, new UserContextService(users));

        var first = await provider.GetCurrentAsync();
        var second = await provider.GetCurrentAsync();

        Assert.Same(first, second);
        Assert.True(first.CanSee(line));
        Assert.Equal(1, users.Lookups);
    }

    [Fact]
    public async Task GetCurrent_WithoutARequest_Fails()
    {
        var provider = new CurrentUserContextProvider(new HttpContextAccessor(), new UserContextService(new CountingUsers()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetCurrentAsync());
    }

    private sealed class CountingUsers(params User[] all) : IUserRepository
    {
        public int Lookups { get; private set; }

        public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
            Task.FromResult(all.FirstOrDefault(u => u.Username == normalizedUsername));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default)
        {
            Lookups++;
            return Task.FromResult(all.FirstOrDefault(u => u.Id == id));
        }
    }
}
