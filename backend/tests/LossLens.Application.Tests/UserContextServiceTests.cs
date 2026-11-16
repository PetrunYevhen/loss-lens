using LossLens.Application.Exceptions;
using LossLens.Application.Services;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests;

public class UserContextServiceTests
{
    private static readonly Guid LineA = Guid.NewGuid();
    private static readonly Guid LineB = Guid.NewGuid();

    private readonly FakeUsers _users = new();

    private UserContextService Service => new(_users);

    private User Add(UserRole role, params Guid[] lines)
    {
        var user = new User($"user{_users.All.Count}", "hash", role, lines);
        _users.All.Add(user);
        return user;
    }

    [Fact]
    public async Task Manager_SeesEveryLine()
    {
        var user = Add(UserRole.Manager);

        var context = await Service.GetAsync(user.Id);

        Assert.True(context.IsManager);
        Assert.Empty(context.LineIds);
    }

    [Fact]
    public async Task Operator_GetsOnlyTheirLine()
    {
        var user = Add(UserRole.Operator, LineA);

        var context = await Service.GetAsync(user.Id);

        Assert.Equal(UserRole.Operator, context.Role);
        Assert.True(context.CanSee(LineA));
        Assert.False(context.CanSee(LineB));
    }

    [Fact]
    public async Task Supervisor_GetsEveryLineOfTheUser()
    {
        var user = Add(UserRole.Supervisor, LineA, LineB);

        var context = await Service.GetAsync(user.Id);

        Assert.Equal([LineA, LineB], context.LineIds);
    }

    [Fact]
    public async Task ChangedAssignment_IsSeenOnTheNextCall()
    {
        var user = Add(UserRole.Supervisor, LineA);
        Assert.False((await Service.GetAsync(user.Id)).CanSee(LineB));

        user.AssignLines([LineA, LineB]);

        Assert.True((await Service.GetAsync(user.Id)).CanSee(LineB));
    }

    [Fact]
    public async Task UnknownUser_IsUnauthorized()
    {
        Add(UserRole.Manager);

        await Assert.ThrowsAsync<UnauthorizedException>(() => Service.GetAsync(Guid.NewGuid()));
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> All { get; } = [];

        public Task<User?> FindByUsernameAsync(string normalizedUsername, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(u => u.Username == normalizedUsername));

        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(u => u.Id == id));
    }
}
