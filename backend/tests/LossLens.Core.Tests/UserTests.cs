using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Core.Tests;

public class UserTests
{
    private static readonly Guid LineA = Guid.NewGuid();
    private static readonly Guid LineB = Guid.NewGuid();

    [Fact]
    public void Username_IsTrimmedAndLowerCased()
    {
        var user = new User("  Operator1 ", "hash", UserRole.Operator, [LineA]);

        Assert.Equal("operator1", user.Username);
    }

    [Fact]
    public void Normalize_MatchesTheStoredForm()
    {
        Assert.Equal("operator1", User.Normalize(" OPERATOR1 "));
    }

    [Fact]
    public void Manager_HasNoLines_EvenWhenGiven()
    {
        var user = new User("boss", "hash", UserRole.Manager, [LineA, LineB]);

        Assert.Empty(user.LineIds);
    }

    [Fact]
    public void Operator_NeedsExactlyOneLine()
    {
        Assert.Equal([LineA], new User("op", "hash", UserRole.Operator, [LineA]).LineIds);
        Assert.Throws<ArgumentException>(() => new User("op", "hash", UserRole.Operator, []));
        Assert.Throws<ArgumentException>(() => new User("op", "hash", UserRole.Operator, [LineA, LineB]));
    }

    [Fact]
    public void Supervisor_NeedsAtLeastOneLine_AndLinesAreDeduplicated()
    {
        var user = new User("sup", "hash", UserRole.Supervisor, [LineA, LineB, LineA]);

        Assert.Equal(2, user.LineIds.Count);
        Assert.Throws<ArgumentException>(() => new User("sup", "hash", UserRole.Supervisor, []));
    }

    [Theory]
    [InlineData("", "hash")]
    [InlineData("  ", "hash")]
    [InlineData("name", "")]
    [InlineData("name", " ")]
    public void BlankUsernameOrHash_IsRejected(string username, string hash)
    {
        Assert.ThrowsAny<ArgumentException>(() => new User(username, hash, UserRole.Manager, []));
    }

    [Fact]
    public void UnknownRole_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new User("name", "hash", (UserRole)99, []));
    }

    [Fact]
    public void EmptyLineId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new User("op", "hash", UserRole.Operator, [Guid.Empty]));
    }

    [Fact]
    public void AssignLines_ReplacesTheLines_WithoutDuplicates()
    {
        var user = new User("sup", "hash", UserRole.Supervisor, [LineA]);

        user.AssignLines([LineA, LineB, LineB]);

        Assert.Equal([LineA, LineB], user.LineIds);
    }

    [Fact]
    public void AssignLines_KeepsTheRulesOfTheRole()
    {
        var op = new User("op", "hash", UserRole.Operator, [LineA]);
        var sup = new User("sup", "hash", UserRole.Supervisor, [LineA]);

        Assert.Throws<ArgumentException>(() => op.AssignLines([LineA, LineB]));
        Assert.Throws<ArgumentException>(() => sup.AssignLines([]));
        Assert.Throws<ArgumentException>(() => sup.AssignLines([Guid.Empty]));

        // A rejected change leaves the old lines.
        Assert.Equal([LineA], op.LineIds);
        Assert.Equal([LineA], sup.LineIds);
    }

    [Fact]
    public void AssignLines_ToAManager_StaysEmpty()
    {
        var boss = new User("boss", "hash", UserRole.Manager, []);

        boss.AssignLines([LineA]);

        Assert.Empty(boss.LineIds);
    }
}
