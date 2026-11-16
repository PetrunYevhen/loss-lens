using LossLens.Api.Auth;
using LossLens.Application.Exceptions;
using LossLens.Core.Models.Enums;

namespace LossLens.Api.Tests;

public class DemoUserContextProviderTests
{
    private static readonly Guid LineA = Guid.NewGuid();
    private static readonly Guid LineB = Guid.NewGuid();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MissingRole_MeansManager(string? role)
    {
        var user = DemoUserContextProvider.Parse(role, null);

        Assert.True(user.IsManager);
    }

    [Theory]
    [InlineData("manager")]
    [InlineData("Manager")]
    [InlineData(" MANAGER ")]
    public void Manager_IsCaseInsensitive_AndIgnoresLineIds(string role)
    {
        var user = DemoUserContextProvider.Parse(role, LineA.ToString());

        Assert.True(user.IsManager);
        Assert.Empty(user.LineIds);
    }

    [Fact]
    public void Operator_NeedsExactlyOneLine()
    {
        var user = DemoUserContextProvider.Parse("operator", LineA.ToString());

        Assert.Equal(UserRole.Operator, user.Role);
        Assert.Equal([LineA], user.LineIds);
        Assert.Throws<ValidationException>(() => DemoUserContextProvider.Parse("operator", null));
        Assert.Throws<ValidationException>(() => DemoUserContextProvider.Parse("operator", $"{LineA},{LineB}"));
    }

    [Fact]
    public void Supervisor_AcceptsSeveralLines_AndTrimsAndDeduplicates()
    {
        var user = DemoUserContextProvider.Parse("supervisor", $" {LineA} , {LineB},{LineA}");

        Assert.Equal(UserRole.Supervisor, user.Role);
        Assert.Equal(new[] { LineA, LineB }, user.LineIds);
    }

    [Fact]
    public void Supervisor_NeedsAtLeastOneLine()
    {
        Assert.Throws<ValidationException>(() => DemoUserContextProvider.Parse("supervisor", ""));
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("1")]
    public void UnknownRole_IsInvalid(string role)
    {
        Assert.Throws<ValidationException>(() => DemoUserContextProvider.Parse(role, null));
    }

    [Fact]
    public void MalformedLineId_IsInvalid()
    {
        Assert.Throws<ValidationException>(() => DemoUserContextProvider.Parse("supervisor", "not-a-guid"));
    }
}
