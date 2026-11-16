using LossLens.Application.Exceptions;
using LossLens.Application.Services;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests;

public class AuthServiceTests
{
    private static readonly Guid LineId = Guid.NewGuid();
    private static readonly DateTimeOffset Expires = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUsers _users = new();
    private readonly FakeHasher _hasher = new();
    private readonly FakeIssuer _issuer = new();

    private AuthService Service => new(_users, _hasher, _issuer);

    private User AddUser(string name, string password, UserRole role, params Guid[] lines)
    {
        var user = new User(name, _hasher.Hash(password), role, lines);
        _users.All.Add(user);
        return user;
    }

    [Fact]
    public async Task ValidCredentials_ReturnTokenRoleAndLines()
    {
        AddUser("operator1", "secret-pass", UserRole.Operator, LineId);

        var result = await Service.LoginAsync("operator1", "secret-pass");

        Assert.Equal("token-for-operator1", result.AccessToken);
        Assert.Equal(Expires, result.ExpiresAt);
        Assert.Equal(UserRole.Operator, result.Role);
        Assert.Equal([LineId], result.LineIds);
    }

    [Fact]
    public async Task Manager_GetsNoLines()
    {
        AddUser("boss", "secret-pass", UserRole.Manager);

        var result = await Service.LoginAsync("boss", "secret-pass");

        Assert.Equal(UserRole.Manager, result.Role);
        Assert.Empty(result.LineIds);
    }

    [Theory]
    [InlineData("OPERATOR1")]
    [InlineData("  operator1  ")]
    public async Task Username_IsNormalizedBeforeTheLookup(string typed)
    {
        AddUser("operator1", "secret-pass", UserRole.Operator, LineId);

        var result = await Service.LoginAsync(typed, "secret-pass");

        Assert.Equal("token-for-operator1", result.AccessToken);
    }

    [Fact]
    public async Task WrongPassword_IsUnauthorized()
    {
        AddUser("operator1", "secret-pass", UserRole.Operator, LineId);

        await Assert.ThrowsAsync<UnauthorizedException>(() => Service.LoginAsync("operator1", "other"));
        Assert.Equal(0, _issuer.Issued);
    }

    [Fact]
    public async Task UnknownUser_GetsTheSameMessageAsAWrongPassword()
    {
        AddUser("operator1", "secret-pass", UserRole.Operator, LineId);

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedException>(() => Service.LoginAsync("operator1", "other"));
        var unknownUser = await Assert.ThrowsAsync<UnauthorizedException>(() => Service.LoginAsync("nobody", "secret-pass"));

        Assert.Equal(wrongPassword.Message, unknownUser.Message);
    }

    [Fact]
    public async Task UnknownUser_StillRunsThePasswordCheck()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Service.LoginAsync("nobody", "secret-pass"));

        // The hasher got a null hash: it does the full work, so timing does not reveal missing users.
        Assert.Equal([null], _hasher.VerifiedHashes);
    }

    [Theory]
    [InlineData(null, "pass")]
    [InlineData("", "pass")]
    [InlineData("  ", "pass")]
    [InlineData("operator1", null)]
    [InlineData("operator1", "")]
    public async Task MissingCredentials_AreUnauthorized(string? username, string? password)
    {
        AddUser("operator1", "secret-pass", UserRole.Operator, LineId);

        await Assert.ThrowsAsync<UnauthorizedException>(() => Service.LoginAsync(username, password));
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> All { get; } = [];

        public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(u => u.Username == normalizedUsername));
    }

    private sealed class FakeHasher : IPasswordHasher
    {
        public List<string?> VerifiedHashes { get; } = [];

        public string Hash(string password) => "hash:" + password;

        public bool Verify(string password, string? hash)
        {
            VerifiedHashes.Add(hash);
            return hash is not null && hash == Hash(password);
        }
    }

    private sealed class FakeIssuer : ITokenIssuer
    {
        public int Issued { get; private set; }

        public AccessToken Issue(User user)
        {
            Issued++;
            return new AccessToken("token-for-" + user.Username, Expires);
        }
    }
}
