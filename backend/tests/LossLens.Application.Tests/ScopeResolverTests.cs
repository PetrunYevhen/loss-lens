using LossLens.Application.Exceptions;
using LossLens.Application.Security;
using LossLens.Application.Tests.Support;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests;

public class ScopeResolverTests
{
    private readonly ScopeResolver _resolver = new();
    private readonly TestWorld _world = new();

    [Fact]
    public void Manager_WithoutLineId_GetsAllLines()
    {
        Assert.Null(_resolver.Resolve(UserContext.Manager, null));
    }

    [Fact]
    public void Manager_WithLineId_GetsThatLine()
    {
        var result = _resolver.Resolve(UserContext.Manager, _world.Line2.Id);

        Assert.Equal([_world.Line2.Id], result);
    }

    [Fact]
    public void Operator_WithoutLineId_GetsOwnLines()
    {
        var result = _resolver.Resolve(TestWorld.Operator(_world.Line1), null);

        Assert.Equal([_world.Line1.Id], result);
    }

    [Fact]
    public void Operator_WithOwnLineId_GetsThatLine()
    {
        var result = _resolver.Resolve(TestWorld.Operator(_world.Line1), _world.Line1.Id);

        Assert.Equal([_world.Line1.Id], result);
    }

    [Fact]
    public void Operator_WithForeignLineId_IsForbidden()
    {
        Assert.Throws<ForbiddenException>(() => _resolver.Resolve(TestWorld.Operator(_world.Line1), _world.Line2.Id));
    }

    [Fact]
    public void Supervisor_WithoutLineId_GetsAllAssignedLines()
    {
        var result = _resolver.Resolve(TestWorld.Supervisor(_world.Line1, _world.Line2), null);

        Assert.Equal(new[] { _world.Line1.Id, _world.Line2.Id }, result);
    }

    [Fact]
    public void OperatorWithoutLines_GetsEmptyScope()
    {
        var result = _resolver.Resolve(new UserContext(UserRole.Operator, []), null);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void NullUser_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(null!, null));
    }
}
