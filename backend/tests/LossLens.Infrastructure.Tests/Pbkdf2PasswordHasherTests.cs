using LossLens.Infrastructure.Security;

namespace LossLens.Infrastructure.Tests;

public class Pbkdf2PasswordHasherTests
{
    // A low cost keeps the tests fast. The production default is 600,000.
    private readonly Pbkdf2PasswordHasher _hasher = new(iterations: 1_000);

    [Fact]
    public void CorrectPassword_Verifies()
    {
        var hash = _hasher.Hash("correct horse");

        Assert.True(_hasher.Verify("correct horse", hash));
    }

    [Fact]
    public void WrongPassword_DoesNotVerify()
    {
        var hash = _hasher.Hash("correct horse");

        Assert.False(_hasher.Verify("battery staple", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Fact]
    public void SamePassword_GivesDifferentHashesBecauseOfTheSalt()
    {
        Assert.NotEqual(_hasher.Hash("same"), _hasher.Hash("same"));
    }

    [Fact]
    public void Hash_DoesNotContainThePassword_AndHasTheVersionedFormat()
    {
        var hash = _hasher.Hash("topsecret-value");

        Assert.DoesNotContain("topsecret", hash);
        Assert.StartsWith("v1.1000.", hash);
        Assert.Equal(4, hash.Split('.').Length);
    }

    [Fact]
    public void Hash_RemembersItsOwnCost()
    {
        var hash = new Pbkdf2PasswordHasher(iterations: 2_000).Hash("pw");

        // A hasher configured with another cost still verifies the old hash.
        Assert.True(_hasher.Verify("pw", hash));
    }

    [Fact]
    public void NullHash_NeverVerifies()
    {
        Assert.False(_hasher.Verify("anything", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("v1.1000.AAAA")]
    [InlineData("v2.1000.AAAA.BBBB")]
    [InlineData("v1.abc.AAAA.BBBB")]
    [InlineData("v1.0.AAAA.BBBB")]
    [InlineData("v1.1000.!!!.!!!")]
    public void MalformedHash_DoesNotVerify(string hash)
    {
        Assert.False(_hasher.Verify("pw", hash));
    }

    [Fact]
    public void EmptyPassword_CannotBeHashed()
    {
        Assert.Throws<ArgumentException>(() => _hasher.Hash(""));
    }
}
