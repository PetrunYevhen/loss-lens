using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using LossLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LossLens.Api.Tests.Integration;

/// <summary>
/// The access token says who the user is. The lines come from the database on every request, so a new line
/// or a changed assignment works with the token the user already has.
/// Each test makes its own user and removes everything it added, the other tests see the usual demo data.
/// </summary>
[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class UserLinesEndpointTests(ApiFixture api)
{
    private const string Password = "Another-Password-1";

    private async Task<List<Guid>> PlantLineIdsAsync()
    {
        using var client = await api.CreateClientAsync("manager");
        var lines = await (await client.GetAsync(ApiUrls.Lines)).ReadJsonAsync();
        return lines.EnumerateArray().Select(l => l.GetProperty("id").GetGuid()).ToList();
    }

    /// <summary>Makes a supervisor and signs in. The token is the only thing the user keeps.</summary>
    private async Task<(Guid UserId, HttpClient Client)> SignInNewSupervisorAsync(params Guid[] lineIds)
    {
        var username = $"temp-{Guid.NewGuid():N}";

        var userId = await api.InScopeAsync(async services =>
        {
            var db = services.GetRequiredService<LossLensDbContext>();
            var hash = services.GetRequiredService<IPasswordHasher>().Hash(Password);
            var user = new User(username, hash, UserRole.Supervisor, lineIds);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user.Id;
        });

        using var anonymous = api.CreateClient();
        using var login = await anonymous.PostAsJsonAsync(ApiUrls.Login, new { username, password = Password });
        login.EnsureSuccessStatusCode();

        var client = api.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", (await login.ReadJsonAsync()).GetProperty("accessToken").GetString());

        return (userId, client);
    }

    private Task AssignLinesAsync(Guid userId, params Guid[] lineIds) =>
        api.InScopeAsync(async services =>
        {
            var db = services.GetRequiredService<LossLensDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == userId);
            user.AssignLines(lineIds);
            await db.SaveChangesAsync();
            return 0;
        });

    private Task DeleteUserAsync(Guid userId) =>
        api.InScopeAsync(async services =>
            await services.GetRequiredService<LossLensDbContext>().Users.Where(u => u.Id == userId).ExecuteDeleteAsync());

    private Task<Guid> AddLineAsync(string name) =>
        api.InScopeAsync(async services =>
        {
            var db = services.GetRequiredService<LossLensDbContext>();
            var line = new ProductionLine(name);
            db.ProductionLines.Add(line);
            db.Stations.AddRange(
                new Station(line.Id, $"SPI-{name}", StationType.Spi),
                new Station(line.Id, $"AOI-{name}", StationType.Aoi),
                new Station(line.Id, $"ICT-{name}", StationType.Ict),
                new Station(line.Id, $"FCT-{name}", StationType.Fct));
            await db.SaveChangesAsync();
            return line.Id;
        });

    private Task DeleteLineAsync(Guid lineId) =>
        api.InScopeAsync(async services =>
        {
            var db = services.GetRequiredService<LossLensDbContext>();
            await db.Stations.Where(s => s.LineId == lineId).ExecuteDeleteAsync();
            return await db.ProductionLines.Where(l => l.Id == lineId).ExecuteDeleteAsync();
        });

    private static async Task<List<JsonElement>> LinesAsync(HttpClient client)
    {
        using var response = await client.GetAsync(ApiUrls.Lines);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.ReadJsonAsync()).EnumerateArray().ToList();
    }

    [Fact]
    public async Task NewLine_AssignedToAUser_IsVisibleWithTheSameToken()
    {
        var existing = await PlantLineIdsAsync();
        var (userId, client) = await SignInNewSupervisorAsync(existing[0]);
        Guid? newLine = null;
        try
        {
            Assert.Single(await LinesAsync(client));

            newLine = await AddLineAsync("Line 4");
            await AssignLinesAsync(userId, existing[0], newLine.Value);

            // The same token, no new sign-in.
            var lines = await LinesAsync(client);
            Assert.Equal(2, lines.Count);
            var added = lines.Single(l => l.GetProperty("id").GetGuid() == newLine);
            Assert.Equal("Line 4", added.GetProperty("name").GetString());
            Assert.Equal(4, added.GetProperty("stations").GetArrayLength());

            // A line without data yet is empty, not an error.
            using var kpi = await client.GetAsync(ApiUrls.Kpi(api.Yesterday, newLine));
            Assert.Equal(HttpStatusCode.OK, kpi.StatusCode);
        }
        finally
        {
            client.Dispose();
            await DeleteUserAsync(userId);
            if (newLine is not null)
                await DeleteLineAsync(newLine.Value);
        }
    }

    [Fact]
    public async Task NewLine_IsVisibleToAManagerAtOnce()
    {
        var newLine = await AddLineAsync("Line 5");
        try
        {
            using var manager = await api.CreateClientAsync("manager");

            var lines = await LinesAsync(manager);

            Assert.Contains(lines, l => l.GetProperty("id").GetGuid() == newLine);
        }
        finally
        {
            await DeleteLineAsync(newLine);
        }
    }

    [Fact]
    public async Task NewLine_IsNotVisibleToAnOperatorOfAnotherLine()
    {
        var newLine = await AddLineAsync("Line 6");
        try
        {
            using var client = await api.CreateClientAsync("operator1");

            var lines = await LinesAsync(client);
            using var response = await client.GetAsync(ApiUrls.Kpi(api.Yesterday, newLine));

            Assert.DoesNotContain(lines, l => l.GetProperty("id").GetGuid() == newLine);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
        finally
        {
            await DeleteLineAsync(newLine);
        }
    }

    [Fact]
    public async Task LineTakenAway_IsForbiddenWithTheSameToken()
    {
        var existing = await PlantLineIdsAsync();
        var (userId, client) = await SignInNewSupervisorAsync(existing[0], existing[1]);
        try
        {
            using var before = await client.GetAsync(ApiUrls.Kpi(api.Yesterday, existing[1]));
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

            await AssignLinesAsync(userId, existing[0]);

            using var after = await client.GetAsync(ApiUrls.Kpi(api.Yesterday, existing[1]));
            Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
            Assert.Single(await LinesAsync(client));
        }
        finally
        {
            client.Dispose();
            await DeleteUserAsync(userId);
        }
    }

    [Fact]
    public async Task RemovedUser_LosesAccessAtOnce_EvenWithAValidToken()
    {
        var existing = await PlantLineIdsAsync();
        var (userId, client) = await SignInNewSupervisorAsync(existing[0]);
        try
        {
            using var before = await client.GetAsync(ApiUrls.Lines);
            Assert.Equal(HttpStatusCode.OK, before.StatusCode);

            await DeleteUserAsync(userId);

            using var after = await client.GetAsync(ApiUrls.Lines);
            Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
        }
        finally
        {
            client.Dispose();
            await DeleteUserAsync(userId);
        }
    }

    [Fact]
    public async Task Token_HasNoLineClaims()
    {
        var token = new JsonWebToken(await api.GetTokenAsync("operator1"));

        Assert.DoesNotContain(token.Claims, c => c.Type == "line_id");
        Assert.Equal("operator", token.GetClaim("role").Value);
    }
}
