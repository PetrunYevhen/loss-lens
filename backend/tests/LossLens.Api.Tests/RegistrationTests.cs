using System.Net;
using LossLens.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LossLens.Api.Tests;

public class RegistrationTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.1.2.3")]
    [InlineData("172.18.0.5")]
    [InlineData("192.168.1.20")]
    public void ProxyHeaders_AreTrustedFromThisMachineAndPrivateNetworks(string address)
    {
        Assert.True(IsTrusted(address));
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("172.32.0.1")]
    [InlineData("203.0.113.9")]
    public void ProxyHeaders_AreNotTrustedFromTheInternet(string address)
    {
        Assert.False(IsTrusted(address));
    }

    [Theory]
    [InlineData("0", "60")]
    [InlineData("10", "0")]
    [InlineData("-1", "60")]
    public void LoginRateLimit_RejectsNonsenseSettings(string permitLimit, string windowSeconds)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RateLimiting:Login:PermitLimit"] = permitLimit,
            ["RateLimiting:Login:WindowSeconds"] = windowSeconds
        }).Build();

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddLoginRateLimit(configuration));
    }

    [Fact]
    public void LoginRateLimit_AcceptsTheDefaults()
    {
        var configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection().AddLoginRateLimit(configuration);

        Assert.NotEmpty(services);
    }

    private static bool IsTrusted(string address)
    {
        using var provider = new ServiceCollection().AddProxyHeaders().BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;
        var ip = IPAddress.Parse(address);

        return options.KnownNetworks.Any(network => network.Contains(ip));
    }
}
