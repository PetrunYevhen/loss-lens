using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LossLens.Api.Configuration;

public static class RateLimitRegistration
{
    /// <summary>Name of the policy for <c>POST /auth/login</c>, used with [EnableRateLimiting].</summary>
    public const string LoginPolicy = "login";

    /// <summary>
    /// Limits the sign-in attempts of one client address: <c>RateLimiting:Login:PermitLimit</c> attempts (10)
    /// in <c>RateLimiting:Login:WindowSeconds</c> seconds (60). The password hash is slow on purpose, this stops
    /// a client from trying passwords one after another. Rejected attempts get 429 with a Retry-After header.
    /// </summary>
    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("RateLimiting:Login:PermitLimit", 10);
        var windowSeconds = configuration.GetValue("RateLimiting:Login:WindowSeconds", 60);

        if (permitLimit < 1 || windowSeconds < 1)
            throw new InvalidOperationException(
                "RateLimiting:Login:PermitLimit and RateLimiting:Login:WindowSeconds must be at least 1.");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    QueueLimit = 0
                }));

            options.OnRejected = async (context, _) =>
            {
                var http = context.HttpContext;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await http.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = http,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Detail = "Too many sign-in attempts. Try again later."
                        }
                    });
            };
        });

        return services;
    }
}
