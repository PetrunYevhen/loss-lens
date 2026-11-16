using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace LossLens.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class AuthEndpointTests(ApiFixture api)
{
    private const string Day = "2026-01-01";

    public static TheoryData<string> ProtectedUrls =>
    [
        ApiUrls.Lines,
        ApiUrls.Shifts,
        ApiUrls.Thresholds,
        $"/api/v1/kpi/summary?date={Day}",
        $"/api/v1/plan-vs-actual?date={Day}",
        $"/api/v1/fpy/stations?date={Day}",
        $"/api/v1/fpy/trend?from={Day}&to={Day}",
        $"/api/v1/defects/pareto?from={Day}&to={Day}"
    ];

    [Fact]
    public async Task Login_AsManager_ReturnsTokenWithoutLines()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            ApiUrls.Login, new { username = "manager", password = ApiFixture.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal("manager", body.GetProperty("role").GetString());
        Assert.Empty(body.GetProperty("lineIds").EnumerateArray());
        Assert.True(body.GetProperty("expiresAt").GetDateTimeOffset() > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Login_AsOperator_ReturnsTheOperatorsLine()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            ApiUrls.Login, new { username = "operator1", password = ApiFixture.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Equal("operator", body.GetProperty("role").GetString());
        Assert.Single(body.GetProperty("lineIds").EnumerateArray());
    }

    [Fact]
    public async Task Login_UserNameIsNotCaseSensitive()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            ApiUrls.Login, new { username = "  Manager ", password = ApiFixture.DemoPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPasswordAndUnknownUser_GiveTheSameAnswer()
    {
        using var client = api.CreateClient();

        using var wrongPassword = await client.PostAsJsonAsync(
            ApiUrls.Login, new { username = "manager", password = "not-the-password" });
        using var unknownUser = await client.PostAsJsonAsync(
            ApiUrls.Login, new { username = "nobody", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        Assert.Equal(
            (await wrongPassword.ReadJsonAsync()).GetProperty("detail").GetString(),
            (await unknownUser.ReadJsonAsync()).GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("manager", "")]
    [InlineData("   ", "whatever")]
    public async Task Login_BlankCredentials_Is401(string username, string password)
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync(ApiUrls.Login, new { username, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"username":"manager"}""")]
    [InlineData("not json")]
    public async Task Login_MalformedBody_Is400(string body)
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsync(
            ApiUrls.Login, new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedUrls))]
    public async Task ProtectedEndpoint_WithoutToken_Is401(string url)
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_ThatIsNotAJwt_Is401()
    {
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_SignedWithAnotherKey_Is401_EvenWhenItClaimsToBeManager()
    {
        var now = DateTime.UtcNow;
        var manager = ApiFixture.SubjectOf(await api.GetTokenAsync("manager"));
        var forged = ApiFixture.CreateToken(
            "another-signing-key-another-signing-key", "manager", now, now.AddHours(1), manager);
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", forged);

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_ThatExpired_Is401()
    {
        var now = DateTime.UtcNow;
        var manager = ApiFixture.SubjectOf(await api.GetTokenAsync("manager"));
        var expired = ApiFixture.CreateToken(ApiFixture.JwtKey, "manager", now.AddHours(-2), now.AddHours(-1), manager);
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expired);

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_OfAUserThatDoesNotExist_Is401()
    {
        var now = DateTime.UtcNow;
        var token = ApiFixture.CreateToken(ApiFixture.JwtKey, "manager", now, now.AddHours(1));
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Role_ComesFromTheDatabase_NotFromTheTokenClaim()
    {
        // Signed with the real key, but the claim says manager while the user is an operator.
        var now = DateTime.UtcNow;
        var operatorId = ApiFixture.SubjectOf(await api.GetTokenAsync("operator1"));
        var token = ApiFixture.CreateToken(ApiFixture.JwtKey, "manager", now, now.AddHours(1), operatorId);
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single((await response.ReadJsonAsync()).EnumerateArray());
    }

    [Fact]
    public async Task Token_BuiltByTheTestHelper_IsAcceptedWhenSignedWithTheRealKey()
    {
        // Guards the negative tests above: the helper makes valid tokens, so they fail only for their own reason.
        var now = DateTime.UtcNow;
        var manager = ApiFixture.SubjectOf(await api.GetTokenAsync("manager"));
        var token = ApiFixture.CreateToken(ApiFixture.JwtKey, "manager", now, now.AddHours(1), manager);
        using var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.GetAsync(ApiUrls.Lines);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_NeedsNoToken()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(ApiUrls.Health);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
