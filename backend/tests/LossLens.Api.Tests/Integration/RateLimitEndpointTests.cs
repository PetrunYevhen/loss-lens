using System.Net;
using System.Net.Http.Json;

namespace LossLens.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class RateLimitEndpointTests(ApiFixture api)
{
    [Fact]
    public async Task Login_AfterTooManyAttempts_Is429WithRetryAfter()
    {
        await using var factory = api.CreateFactoryWith(
            ("RateLimiting__Login__PermitLimit", "3"), ("RateLimiting__Login__WindowSeconds", "60"));
        using var client = factory.CreateClient();
        var wrong = new { username = "manager", password = "not-the-password" };

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var allowed = await client.PostAsJsonAsync(ApiUrls.Login, wrong);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var limited = await client.PostAsJsonAsync(ApiUrls.Login, wrong);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.Equal("application/problem+json", limited.Content.Headers.ContentType?.MediaType);
        var retryAfter = Assert.Single(limited.Headers.GetValues("Retry-After"));
        Assert.InRange(int.Parse(retryAfter), 1, 60);
    }

    [Fact]
    public async Task Login_WhenLimited_EvenTheRightPasswordWaits()
    {
        await using var factory = api.CreateFactoryWith(("RateLimiting__Login__PermitLimit", "2"));
        using var client = factory.CreateClient();
        var wrong = new { username = "manager", password = "not-the-password" };
        var right = new { username = "manager", password = ApiFixture.DemoPassword };

        (await client.PostAsJsonAsync(ApiUrls.Login, wrong)).Dispose();
        (await client.PostAsJsonAsync(ApiUrls.Login, wrong)).Dispose();

        using var response = await client.PostAsJsonAsync(ApiUrls.Login, right);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_AreNotLimited()
    {
        await using var factory = api.CreateFactoryWith(("RateLimiting__Login__PermitLimit", "1"));
        using var client = factory.CreateClient();
        (await client.PostAsJsonAsync(ApiUrls.Login, new { username = "x", password = "y" })).Dispose();
        (await client.PostAsJsonAsync(ApiUrls.Login, new { username = "x", password = "y" })).Dispose();

        for (var i = 0; i < 5; i++)
        {
            using var health = await client.GetAsync(ApiUrls.Health);
            Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        }
    }
}
