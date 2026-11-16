using System.Net;
using System.Text.Json;

namespace LossLens.Api.Tests.Integration;

/// <summary>The API description and the Swagger page that lets a developer try the endpoints.</summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class OpenApiEndpointTests(ApiFixture api)
{
    private const string LoginPath = "/api/v1/auth/login";

    private async Task<JsonElement> DocumentAsync()
    {
        using var client = api.CreateClient();
        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    [Fact]
    public async Task Document_NeedsNoToken_AndHasATitle()
    {
        var document = await DocumentAsync();

        Assert.Equal("LossLens API", document.GetProperty("info").GetProperty("title").GetString());
        Assert.Equal("v1", document.GetProperty("info").GetProperty("version").GetString());
    }

    [Fact]
    public async Task Document_DescribesEveryEndpoint()
    {
        var paths = (await DocumentAsync()).GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();

        Assert.Equivalent(
            new[]
            {
                LoginPath, ApiUrls.Lines, ApiUrls.Shifts, ApiUrls.Thresholds,
                "/api/v1/kpi/summary", "/api/v1/plan-vs-actual",
                "/api/v1/fpy/trend", "/api/v1/fpy/stations", "/api/v1/defects/pareto"
            },
            paths);
    }

    [Fact]
    public async Task Document_HasTheBearerScheme_ForTheAuthorizeButton()
    {
        var scheme = (await DocumentAsync()).GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");

        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());
    }

    [Fact]
    public async Task EveryOperation_AsksForABearerToken_ExceptSignIn()
    {
        var paths = (await DocumentAsync()).GetProperty("paths");

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var operation in path.Value.EnumerateObject())
            {
                var requirements = operation.Value.TryGetProperty("security", out var security)
                    ? security.EnumerateArray().ToList()
                    : [];

                if (path.Name == LoginPath)
                {
                    Assert.Empty(requirements);
                }
                else
                {
                    var requirement = Assert.Single(requirements);
                    Assert.True(requirement.TryGetProperty("Bearer", out _), $"{operation.Name} {path.Name}");
                }
            }
        }
    }

    [Fact]
    public async Task SwaggerPage_NeedsNoToken()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("swagger-ui", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task WhenSwitchedOff_ThePageAndTheDocumentAreGone()
    {
        await using var factory = api.CreateFactoryWith(("Swagger__Enabled", "false"));
        using var anonymous = factory.CreateClient();
        using var signedIn = factory.CreateClient();
        signedIn.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await api.GetTokenAsync("manager"));

        foreach (var path in new[] { "/swagger/index.html", "/openapi/v1.json" })
        {
            // Not found for a signed-in user. Nobody without a token gets anything, not even a hint.
            using var withToken = await signedIn.GetAsync(path);
            using var withoutToken = await anonymous.GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, withToken.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, withoutToken.StatusCode);
        }
    }
}
