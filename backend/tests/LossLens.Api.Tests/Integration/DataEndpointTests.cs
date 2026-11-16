using System.Net;
using System.Text.Json;

namespace LossLens.Api.Tests.Integration;

/// <summary>The shape and the arithmetic of the answers, from the database up to the JSON.</summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class DataEndpointTests(ApiFixture api)
{
    private static readonly string[] Statuses = ["unknown", "green", "amber", "red"];

    private async Task<JsonElement> GetAsync(string url, string user = "manager")
    {
        using var client = await api.CreateClientAsync(user);
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private async Task<Guid> ShiftIdAsync(string name)
    {
        var shifts = await GetAsync(ApiUrls.Shifts);
        return shifts.EnumerateArray().Single(s => s.GetProperty("name").GetString() == name).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Shifts_AreAB_AndC_WithC_RunningThroughMidnight()
    {
        var shifts = (await GetAsync(ApiUrls.Shifts)).EnumerateArray().ToList();

        Assert.Equal(["A", "B", "C"], shifts.Select(s => s.GetProperty("name").GetString()));
        Assert.Equal(["06:00", "14:00", "22:00"], shifts.Select(s => s.GetProperty("start").GetString()));
        Assert.Equal(["14:00", "22:00", "06:00"], shifts.Select(s => s.GetProperty("end").GetString()));
    }

    [Fact]
    public async Task Thresholds_HaveTheDefaultLimits()
    {
        var body = await GetAsync(ApiUrls.Thresholds);

        Assert.Equal((95, 85), Limits(body, "planAttainment"));
        Assert.Equal((92, 88), Limits(body, "lineFpy"));
        Assert.Equal((97, 94), Limits(body, "stationFpy"));

        static (double Green, double Amber) Limits(JsonElement root, string name) =>
            (root.GetProperty(name).GetProperty("green").GetDouble(), root.GetProperty(name).GetProperty("amber").GetDouble());
    }

    [Fact]
    public async Task Kpi_ForAFullDay_HasFourFilledCards()
    {
        var body = await GetAsync(ApiUrls.Kpi(api.Yesterday));

        Assert.Equal(ApiUrls.Date(api.Yesterday), body.GetProperty("date").GetString());

        var plan = body.GetProperty("planAttainment");
        Assert.InRange(plan.GetProperty("value").GetDouble(), 1, 200);
        Assert.Equal(95, plan.GetProperty("target").GetDouble());
        Assert.Contains(plan.GetProperty("status").GetString(), Statuses);

        var rty = body.GetProperty("lineRty");
        Assert.InRange(rty.GetProperty("value").GetDouble(), 1, 100);
        Assert.False(rty.GetProperty("incomplete").GetBoolean());
        Assert.Contains(rty.GetProperty("status").GetString(), Statuses);

        Assert.True(body.GetProperty("dpmo").GetProperty("value").GetDouble() > 0);

        var top = body.GetProperty("topDefect");
        Assert.False(string.IsNullOrEmpty(top.GetProperty("code").GetString()));
        Assert.True(top.GetProperty("count").GetInt32() > 0);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(-1000)]
    public async Task Kpi_ForADayWithoutData_IsEmptyAndUnknown(int daysFromYesterday)
    {
        var body = await GetAsync(ApiUrls.Kpi(api.Yesterday.AddDays(daysFromYesterday)));

        var plan = body.GetProperty("planAttainment");
        Assert.Equal(JsonValueKind.Null, plan.GetProperty("value").ValueKind);
        Assert.Equal("unknown", plan.GetProperty("status").GetString());

        var rty = body.GetProperty("lineRty");
        Assert.Equal(JsonValueKind.Null, rty.GetProperty("value").ValueKind);
        Assert.True(rty.GetProperty("incomplete").GetBoolean());

        Assert.Equal(JsonValueKind.Null, body.GetProperty("dpmo").GetProperty("value").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("topDefect").ValueKind);
    }

    [Fact]
    public async Task Plan_ForAFullDay_HasAnHourlyPointForEveryHour()
    {
        var points = (await GetAsync(ApiUrls.Plan(api.Yesterday))).EnumerateArray().ToList();

        Assert.Equal(Enumerable.Range(0, 24), points.Select(p => p.GetProperty("hour").GetInt32()));
        Assert.All(points, p =>
        {
            Assert.True(p.GetProperty("planned").GetInt32() > 0);
            Assert.True(p.GetProperty("good").GetInt32() >= 0);
        });
    }

    [Theory]
    [InlineData("A", new[] { 6, 7, 8, 9, 10, 11, 12, 13 })]
    [InlineData("B", new[] { 14, 15, 16, 17, 18, 19, 20, 21 })]
    [InlineData("C", new[] { 0, 1, 2, 3, 4, 5, 22, 23 })]
    public async Task Plan_ForAShift_HasOnlyTheHoursOfThatShift(string shift, int[] hours)
    {
        var shiftId = await ShiftIdAsync(shift);

        var points = (await GetAsync(ApiUrls.Plan(api.Yesterday, shiftId: shiftId))).EnumerateArray();

        Assert.Equal(hours, points.Select(p => p.GetProperty("hour").GetInt32()).Order());
    }

    [Fact]
    public async Task FpyStations_AreTheFourTypes_AndFpyIsFirstPassOverTested()
    {
        var stations = (await GetAsync(ApiUrls.FpyStations(api.Yesterday))).EnumerateArray().ToList();

        Assert.Equal(["SPI", "AOI", "ICT", "FCT"], stations.Select(s => s.GetProperty("stationId").GetString()));
        Assert.All(stations, s =>
        {
            var tested = s.GetProperty("tested").GetInt32();
            var firstPass = s.GetProperty("firstPass").GetInt32();

            Assert.True(tested > 0);
            Assert.InRange(firstPass, 0, tested);

            // From the summed counts, not from an average of hourly percentages.
            Assert.Equal(100.0 * firstPass / tested, s.GetProperty("fpy").GetDouble(), 6);
            Assert.Contains(s.GetProperty("status").GetString(), Statuses);
        });
    }

    [Fact]
    public async Task FpyTrend_HasOnePointPerDay_InOrder()
    {
        var from = api.Yesterday.AddDays(-6);

        var points = (await GetAsync(ApiUrls.FpyTrend(from, api.Yesterday))).EnumerateArray().ToList();

        Assert.Equal(
            Enumerable.Range(0, 7).Select(i => ApiUrls.Date(from.AddDays(i))),
            points.Select(p => p.GetProperty("date").GetString()));
        Assert.All(points, p => Assert.InRange(p.GetProperty("rty").GetDouble(), 1, 100));
    }

    [Fact]
    public async Task Pareto_IsSortedAndAddsUpTo100Percent()
    {
        var body = await GetAsync(ApiUrls.Pareto(api.Yesterday.AddDays(-6), api.Yesterday));

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.NotEmpty(items);

        var counts = items.Select(i => i.GetProperty("count").GetInt32()).ToList();
        Assert.Equal(counts.OrderByDescending(c => c), counts);

        Assert.Equal(100, items.Sum(i => i.GetProperty("share").GetDouble()), 6);

        var cumulative = items.Select(i => i.GetProperty("cumulativeShare").GetDouble()).ToList();
        Assert.Equal(cumulative.Order(), cumulative);
        Assert.Equal(100, cumulative[^1], 6);

        Assert.True(body.GetProperty("dpmo").GetDouble() > 0);
    }

    [Fact]
    public async Task Pareto_ForOneStation_CountsOnlyThatStation()
    {
        var lines = await GetAsync(ApiUrls.Lines);
        var line = lines[0];
        var station = line.GetProperty("stations")[2].GetProperty("id").GetGuid();

        var whole = await GetAsync(ApiUrls.Pareto(api.Yesterday, api.Yesterday));
        var one = await GetAsync(ApiUrls.Pareto(api.Yesterday, api.Yesterday, stationId: station));

        Assert.InRange(TotalDefects(one), 1, TotalDefects(whole) - 1);

        static int TotalDefects(JsonElement pareto) =>
            pareto.GetProperty("items").EnumerateArray().Sum(i => i.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Kpi_ForOneLine_DiffersFromTheWholePlant()
    {
        var lines = await GetAsync(ApiUrls.Lines);
        var lineId = lines[0].GetProperty("id").GetGuid();

        var whole = await GetAsync(ApiUrls.Kpi(api.Yesterday));
        var one = await GetAsync(ApiUrls.Kpi(api.Yesterday, lineId));

        // Three lines with different random output cannot give the same plan attainment to the last digit.
        Assert.NotEqual(
            whole.GetProperty("planAttainment").GetProperty("value").GetDouble(),
            one.GetProperty("planAttainment").GetProperty("value").GetDouble());
    }

    [Fact]
    public async Task Responses_UseCamelCase()
    {
        var body = await GetAsync(ApiUrls.Kpi(api.Yesterday));

        Assert.True(body.TryGetProperty("planAttainment", out _));
        Assert.False(body.TryGetProperty("PlanAttainment", out _));
    }
}
