using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LossLens.Application.Time;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace LossLens.Api.Tests.Integration;

/// <summary>
/// Starts the real API (all layers, JWT, EF Core migrations, demo data) against a throwaway PostgreSQL
/// container. One instance is shared by every endpoint test class, see <see cref="ApiCollection"/>.
/// Needs Docker.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string DemoPassword = "Integration-Test-1";
    public const string JwtKey = "integration-tests-signing-key-0123456789";
    public const string Issuer = "LossLens";
    public const string Audience = "LossLens.Web";

    // Environment variables win over a developer's backend/.env (see Env.NoClobber in Program.cs),
    // so the tests can never reach a real database.
    private static readonly string[] OwnedVariables =
        ["ConnectionStrings__Default", "Jwt__Key", "Auth__DemoPassword", "Data__SeedMockData"];

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine").Build();
    private readonly Dictionary<string, string?> _previousVariables = [];
    private readonly ConcurrentDictionary<string, string> _tokens = new();
    private WebApplicationFactory<Program>? _factory;

    /// <summary>The last full plant day. Demo data exists for it on every line, shift and hour.</summary>
    public DateOnly Yesterday { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            await _database.StartAsync();

            Set("ConnectionStrings__Default", _database.GetConnectionString());
            Set("Jwt__Key", JwtKey);
            Set("Auth__DemoPassword", DemoPassword);
            Set("Data__SeedMockData", "true");

            // The tests sign in far more often than a person would. One test lowers the limit on its own host.
            Set("RateLimiting__Login__PermitLimit", "1000");

            _factory = new WebApplicationFactory<Program>();

            // Building the host runs the migrations and fills the demo data and demo users.
            var configured = _factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (configured != _database.GetConnectionString())
                throw new InvalidOperationException("The API is not using the test database. Refusing to run.");

            Yesterday = DateOnly.FromDateTime(_factory.Services.GetRequiredService<PlantClock>().Now).AddDays(-1);
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();

        await _database.DisposeAsync();

        foreach (var (name, value) in _previousVariables)
            Environment.SetEnvironmentVariable(name, value);
    }

    /// <summary>
    /// A second API host on the same database with other settings (environment variable names such as
    /// <c>RateLimiting__Login__PermitLimit</c>). The caller disposes it.
    /// </summary>
    public WebApplicationFactory<Program> CreateFactoryWith(params (string Name, string Value)[] settings)
    {
        var previous = settings.Select(s => (s.Name, Value: Environment.GetEnvironmentVariable(s.Name))).ToList();
        foreach (var (name, value) in settings)
            Environment.SetEnvironmentVariable(name, value);

        try
        {
            var factory = new WebApplicationFactory<Program>();
            _ = factory.Server; // builds the host now, while the variables are set
            return factory;
        }
        finally
        {
            foreach (var (name, value) in previous)
                Environment.SetEnvironmentVariable(name, value);
        }
    }

    /// <summary>A client without a token.</summary>
    public HttpClient CreateClient() => _factory!.CreateClient();

    /// <summary>A client signed in as a demo user (manager, operator1, operator2 or operator3).</summary>
    public async Task<HttpClient> CreateClientAsync(string username)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(username));
        return client;
    }

    public async Task<string> GetTokenAsync(string username)
    {
        if (_tokens.TryGetValue(username, out var cached))
            return cached;

        using var client = CreateClient();
        using var response = await client.PostAsJsonAsync(ApiUrls.Login, new { username, password = DemoPassword });
        response.EnsureSuccessStatusCode();

        var token = (await response.ReadJsonAsync()).GetProperty("accessToken").GetString()!;
        return _tokens[username] = token;
    }

    /// <summary>The user id (the <c>sub</c> claim) of a token.</summary>
    public static Guid SubjectOf(string token) => Guid.Parse(new JsonWebToken(token).Subject);

    /// <summary>Runs code against the services of the API, for example to change the database.</summary>
    public async Task<T> InScopeAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _factory!.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    /// <summary>
    /// Builds a token the way the API does, but with the caller's key, times and subject
    /// (a random user id when none is given).
    /// </summary>
    public static string CreateToken(
        string signingKey, string role, DateTime notBefore, DateTime expires, Guid? subject = null)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            NotBefore = notBefore,
            Expires = expires,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = (subject ?? Guid.NewGuid()).ToString(),
                ["name"] = "test",
                ["role"] = role
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    private void Set(string name, string value)
    {
        if (!_previousVariables.ContainsKey(name))
            _previousVariables[name] = Environment.GetEnvironmentVariable(name);

        Environment.SetEnvironmentVariable(name, value);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "Api";
}

public static class HttpResponseExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();
}

/// <summary>The endpoints under test, with the query strings written the way the frontend sends them.</summary>
public static class ApiUrls
{
    public const string Login = "/api/v1/auth/login";
    public const string Lines = "/api/v1/lines";
    public const string Shifts = "/api/v1/shifts";
    public const string Thresholds = "/api/v1/config/thresholds";
    public const string Health = "/health";

    /// <summary>The endpoints that accept a lineId. Used with <see cref="Scoped"/>.</summary>
    public static readonly string[] ScopedEndpoints = ["kpi", "plan", "fpy-stations", "fpy-trend", "pareto"];

    public static string Kpi(DateOnly date, Guid? lineId = null, Guid? shiftId = null) =>
        Build("/api/v1/kpi/summary", ("date", date), ("lineId", lineId), ("shiftId", shiftId));

    public static string Plan(DateOnly date, Guid? lineId = null, Guid? shiftId = null) =>
        Build("/api/v1/plan-vs-actual", ("date", date), ("lineId", lineId), ("shiftId", shiftId));

    public static string FpyStations(DateOnly date, Guid? lineId = null, Guid? shiftId = null) =>
        Build("/api/v1/fpy/stations", ("date", date), ("lineId", lineId), ("shiftId", shiftId));

    public static string FpyTrend(DateOnly from, DateOnly to, Guid? lineId = null) =>
        Build("/api/v1/fpy/trend", ("from", from), ("to", to), ("lineId", lineId));

    public static string Pareto(
        DateOnly from, DateOnly to, Guid? lineId = null, Guid? stationId = null, Guid? shiftId = null) =>
        Build("/api/v1/defects/pareto",
            ("from", from), ("to", to), ("lineId", lineId), ("stationId", stationId), ("shiftId", shiftId));

    /// <summary>One request for each name in <see cref="ScopedEndpoints"/>, for the given day and line.</summary>
    public static string Scoped(string endpoint, DateOnly day, Guid? lineId) => endpoint switch
    {
        "kpi" => Kpi(day, lineId),
        "plan" => Plan(day, lineId),
        "fpy-stations" => FpyStations(day, lineId),
        "fpy-trend" => FpyTrend(day.AddDays(-6), day, lineId),
        "pareto" => Pareto(day, day, lineId),
        _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null)
    };

    public static string Date(DateOnly date) => date.ToString("yyyy-MM-dd");

    private static string Build(string path, params (string Name, object? Value)[] query)
    {
        var parts = query
            .Where(pair => pair.Value is not null)
            .Select(pair => $"{pair.Name}={Uri.EscapeDataString(pair.Value switch
            {
                DateOnly date => Date(date),
                var other => other!.ToString()!
            })}");

        return $"{path}?{string.Join('&', parts)}";
    }
}
