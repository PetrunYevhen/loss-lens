using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace LossLens.Api.Configuration;

public static class ProxyHeadersRegistration
{
    /// <summary>
    /// Behind the nginx container every request comes from the proxy, so the client address is read from
    /// <c>X-Forwarded-For</c>. The header is trusted only when the request itself comes from this machine or a
    /// private network (the Docker network). A client on the internet cannot choose its own address with it.
    /// </summary>
    public static IServiceCollection AddProxyHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var (address, prefix) in PrivateNetworks)
            {
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse(address), prefix));
            }
        });

    private static readonly (string Address, int Prefix)[] PrivateNetworks =
    [
        ("10.0.0.0", 8),
        ("172.16.0.0", 12),
        ("192.168.0.0", 16),
        ("fc00::", 7)
    ];
}
