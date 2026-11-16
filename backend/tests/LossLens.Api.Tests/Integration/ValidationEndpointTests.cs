using System.Net;

namespace LossLens.Api.Tests.Integration;

/// <summary>Bad input is a 400 problem answer, never a 500.</summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ValidationEndpointTests(ApiFixture api)
{
    public static TheoryData<string> BadRequests =>
    [
        // The required date is missing.
        "/api/v1/kpi/summary",
        "/api/v1/plan-vs-actual",
        "/api/v1/fpy/stations",
        "/api/v1/fpy/trend?to=2026-01-02",
        "/api/v1/fpy/trend?from=2026-01-01",
        "/api/v1/defects/pareto?from=2026-01-01",
        "/api/v1/defects/pareto?to=2026-01-02",

        // The date is not a date.
        "/api/v1/kpi/summary?date=2026-13-45",
        "/api/v1/kpi/summary?date=yesterday",
        "/api/v1/plan-vs-actual?date=",
        "/api/v1/defects/pareto?from=abc&to=2026-01-02",

        // An id is not a guid.
        "/api/v1/kpi/summary?date=2026-01-01&lineId=abc",
        "/api/v1/kpi/summary?date=2026-01-01&shiftId=abc",
        "/api/v1/plan-vs-actual?date=2026-01-01&lineId=1",
        "/api/v1/fpy/stations?date=2026-01-01&shiftId=abc",
        "/api/v1/fpy/trend?from=2026-01-01&to=2026-01-02&lineId=abc",
        "/api/v1/defects/pareto?from=2026-01-01&to=2026-01-02&stationId=abc"
    ];

    [Theory]
    [MemberData(nameof(BadRequests))]
    public async Task MissingOrMalformedParameter_Is400(string url)
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task FpyTrend_ReversedRange_Is400()
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(ApiUrls.FpyTrend(api.Yesterday, api.Yesterday.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pareto_ReversedRange_Is400()
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(ApiUrls.Pareto(api.Yesterday, api.Yesterday.AddDays(-1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FpyTrend_RangeLimitIs92Days()
    {
        using var client = await api.CreateClientAsync("manager");
        var to = api.Yesterday;

        using var allowed = await client.GetAsync(ApiUrls.FpyTrend(to.AddDays(-91), to));
        using var tooLong = await client.GetAsync(ApiUrls.FpyTrend(to.AddDays(-92), to));

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Pareto_RangeLimitIs92Days()
    {
        using var client = await api.CreateClientAsync("manager");
        var to = api.Yesterday;

        using var allowed = await client.GetAsync(ApiUrls.Pareto(to.AddDays(-91), to));
        using var tooLong = await client.GetAsync(ApiUrls.Pareto(to.AddDays(-92), to));

        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    public static TheoryData<string> Endpoints => [.. ApiUrls.ScopedEndpoints];

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task UnknownLine_IsNotFound_ForAManager(string endpoint)
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(ApiUrls.Scoped(endpoint, api.Yesterday, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task UnknownLine_IsForbidden_ForAnOperator(string endpoint)
    {
        // The same answer as for a line that exists but belongs to someone else: no hint about what exists.
        using var client = await api.CreateClientAsync("operator1");

        using var response = await client.GetAsync(ApiUrls.Scoped(endpoint, api.Yesterday, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("kpi")]
    [InlineData("plan")]
    [InlineData("fpy-stations")]
    [InlineData("pareto")]
    public async Task UnknownShift_IsNotFound(string endpoint)
    {
        using var client = await api.CreateClientAsync("manager");
        var shift = Guid.NewGuid();
        var day = api.Yesterday;
        var url = endpoint switch
        {
            "kpi" => ApiUrls.Kpi(day, shiftId: shift),
            "plan" => ApiUrls.Plan(day, shiftId: shift),
            "fpy-stations" => ApiUrls.FpyStations(day, shiftId: shift),
            _ => ApiUrls.Pareto(day, day, shiftId: shift)
        };

        using var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Pareto_UnknownStation_IsNotFound()
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync(
            ApiUrls.Pareto(api.Yesterday, api.Yesterday, stationId: Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FpyTrend_UnknownLine_IsNotFound_EvenForTheFuture()
    {
        using var client = await api.CreateClientAsync("manager");
        var future = api.Yesterday.AddDays(10);

        using var response = await client.GetAsync(ApiUrls.FpyTrend(future, future, Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_Is404()
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.GetAsync("/api/v1/nothing-here");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WritingToAReadOnlyEndpoint_Is405()
    {
        using var client = await api.CreateClientAsync("manager");

        using var response = await client.PostAsync(ApiUrls.Lines, content: null);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }
}
