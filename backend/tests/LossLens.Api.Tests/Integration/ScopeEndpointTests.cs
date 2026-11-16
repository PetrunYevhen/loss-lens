using System.Net;
using System.Text.Json;

namespace LossLens.Api.Tests.Integration;

/// <summary>What each role may see: a manager sees every line, an operator only the lines of the token.</summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ScopeEndpointTests(ApiFixture api)
{
    public static TheoryData<string> Endpoints => [.. ApiUrls.ScopedEndpoints];

    private async Task<List<JsonElement>> LinesOfAsync(string username)
    {
        using var client = await api.CreateClientAsync(username);
        using var response = await client.GetAsync(ApiUrls.Lines);
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync()).EnumerateArray().ToList();
    }

    private static Guid IdOf(JsonElement element) => element.GetProperty("id").GetGuid();

    [Fact]
    public async Task Manager_SeesEveryLineWithFourStationsInOrder()
    {
        var lines = await LinesOfAsync("manager");

        Assert.Equal(3, lines.Count);
        foreach (var line in lines)
        {
            var stations = line.GetProperty("stations").EnumerateArray().ToList();
            Assert.Equal(["SPI", "AOI", "ICT", "FCT"], stations.Select(s => s.GetProperty("type").GetString()));
            Assert.Equal([1, 2, 3, 4], stations.Select(s => s.GetProperty("order").GetInt32()));
        }
    }

    [Theory]
    [InlineData("operator1", 0)]
    [InlineData("operator2", 1)]
    public async Task Operator_SeesOnlyTheirOwnLine(string username, int lineIndex)
    {
        var all = await LinesOfAsync("manager");

        var own = await LinesOfAsync(username);

        Assert.Equal([IdOf(all[lineIndex])], own.Select(IdOf));
    }

    [Fact]
    public async Task Supervisor_SeesAllLinesOfTheToken()
    {
        var all = await LinesOfAsync("manager");

        var seen = await LinesOfAsync("supervisor");

        Assert.Equal(all.Select(IdOf).Order(), seen.Select(IdOf).Order());
    }

    [Fact]
    public async Task Operator2_AskingForLine1_Is403()
    {
        var all = await LinesOfAsync("manager");
        using var client = await api.CreateClientAsync("operator2");

        using var response = await client.GetAsync(ApiUrls.Kpi(api.Yesterday, IdOf(all[0])));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Operator_AskingForAnotherLine_Is403(string endpoint)
    {
        var all = await LinesOfAsync("manager");
        using var client = await api.CreateClientAsync("operator1");

        using var response = await client.GetAsync(ApiUrls.Scoped(endpoint, api.Yesterday, IdOf(all[1])));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Operator_AskingForTheirOwnLine_Is200(string endpoint)
    {
        var all = await LinesOfAsync("manager");
        using var client = await api.CreateClientAsync("operator1");

        using var response = await client.GetAsync(ApiUrls.Scoped(endpoint, api.Yesterday, IdOf(all[0])));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Operator_WithoutALine_IsLimitedToTheirOwnLine(string endpoint)
    {
        using var client = await api.CreateClientAsync("operator1");

        using var response = await client.GetAsync(ApiUrls.Scoped(endpoint, api.Yesterday, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Fpy_WithoutALine_ManagerGetsStationTypesAndOperatorGetsOwnStations()
    {
        var all = await LinesOfAsync("manager");
        var ownStationIds = all[0].GetProperty("stations").EnumerateArray()
            .Select(s => s.GetProperty("id").GetString()).ToList();

        using var manager = await api.CreateClientAsync("manager");
        using var operatorClient = await api.CreateClientAsync("operator1");

        var forManager = await (await manager.GetAsync(ApiUrls.FpyStations(api.Yesterday))).ReadJsonAsync();
        var forOperator = await (await operatorClient.GetAsync(ApiUrls.FpyStations(api.Yesterday))).ReadJsonAsync();

        // Several lines are combined per station type, a single line shows its own stations.
        Assert.Equal(["SPI", "AOI", "ICT", "FCT"], forManager.EnumerateArray().Select(s => s.GetProperty("stationId").GetString()));
        Assert.Equal(ownStationIds, forOperator.EnumerateArray().Select(s => s.GetProperty("stationId").GetString()));
    }

    [Fact]
    public async Task Pareto_OperatorAskingForAStationOfAnotherLine_GetsNothing()
    {
        var all = await LinesOfAsync("manager");
        var foreignStation = all[1].GetProperty("stations")[0].GetProperty("id").GetGuid();
        using var client = await api.CreateClientAsync("operator1");

        using var response = await client.GetAsync(ApiUrls.Pareto(api.Yesterday, api.Yesterday, stationId: foreignStation));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.ReadJsonAsync();
        Assert.Empty(body.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("dpmo").ValueKind);
    }

    [Fact]
    public async Task Pareto_StationThatDoesNotBelongToTheChosenLine_GetsNothing()
    {
        var all = await LinesOfAsync("manager");
        var stationOfLine2 = all[1].GetProperty("stations")[0].GetProperty("id").GetGuid();
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(
            ApiUrls.Pareto(api.Yesterday, api.Yesterday, lineId: IdOf(all[0]), stationId: stationOfLine2));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await response.ReadJsonAsync()).GetProperty("items").EnumerateArray());
    }
}
