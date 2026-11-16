using LossLens.Application.Security;
using LossLens.Application.Tests.Support;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests;

public class UserContextTests
{
    private static readonly Guid LineA = Guid.NewGuid();
    private static readonly Guid LineB = Guid.NewGuid();

    [Fact]
    public void Manager_SeesAnyLine()
    {
        Assert.True(UserContext.Manager.IsManager);
        Assert.True(UserContext.Manager.CanSee(Guid.NewGuid()));
    }

    [Fact]
    public void Manager_IgnoresLineIds()
    {
        var manager = new UserContext(UserRole.Manager, [LineA]);

        Assert.True(manager.CanSee(LineB));
    }

    [Fact]
    public void Operator_SeesOnlyOwnLine()
    {
        var user = new UserContext(UserRole.Operator, [LineA]);

        Assert.True(user.CanSee(LineA));
        Assert.False(user.CanSee(LineB));
        Assert.False(user.IsManager);
    }

    [Fact]
    public void Supervisor_SeesAllAssignedLines()
    {
        var user = new UserContext(UserRole.Supervisor, [LineA, LineB]);

        Assert.True(user.CanSee(LineA));
        Assert.True(user.CanSee(LineB));
        Assert.False(user.CanSee(Guid.NewGuid()));
    }

    [Theory]
    [InlineData(UserRole.Operator)]
    [InlineData(UserRole.Supervisor)]
    public void NonManagerWithoutLines_SeesNothing(UserRole role)
    {
        var user = new UserContext(role, []);

        Assert.False(user.CanSee(LineA));
    }

    [Fact]
    public void NullLineIds_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => new UserContext(UserRole.Operator, null!));
    }

    [Fact]
    public void UnknownRole_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UserContext((UserRole)99, []));
    }
}
