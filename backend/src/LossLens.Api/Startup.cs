using System.Text.Json;
using System.Text.Json.Serialization;
using LossLens.Api.Auth;
using LossLens.Api.Configuration;
using LossLens.Api.Errors;
using LossLens.Api.Health;
using LossLens.Application;
using LossLens.Application.Exceptions;
using LossLens.Infrastructure;
using LossLens.Application.Time;
using LossLens.Infrastructure.Mock;
using LossLens.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Api;

public class Startup
{
    private const string FrontendCorsPolicy = "Frontend";

    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

        services.AddOpenApiWithBearer();
        services.AddProblemDetails();
        // services.AddExceptionHandler<ApplicationExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContextProvider, CurrentUserContextProvider>();
        services.AddJwtAuthentication(_configuration);

        services.AddMetricOptions(_configuration);
        services.AddProxyHeaders();
        services.AddLoginRateLimit(_configuration);

        services.AddApplication(_configuration["Plant:TimeZone"]);
        services.AddInfrastructure(
            _configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:Default is not set. Add ConnectionStrings__Default to .env."));

        services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy => policy
            .WithOrigins(_configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
            .AllowAnyHeader()
            .WithMethods(HttpMethods.Get)));

        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");
    }

    public static void Configure(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LossLensDbContext>();
            db.Database.Migrate();

            SeedMockData(app, scope);
            SeedDemoUsers(app, scope);
        }

        // First, so every later step (rate limit, logs) sees the real client address behind the proxy.
        app.UseForwardedHeaders();

        // Unexpected errors become a 500 problem. The middleware inside answers the expected 400, 401 and 403 first.
        app.UseExceptionHandler();
        app.UseMiddleware<ClientErrorMiddleware>();

        // The API description and its browser UI (/swagger). On in development, Swagger:Enabled changes it.
        // The page and the document need no token, but the calls made from the page do.
        if (app.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
        {
            app.MapOpenApi().AllowAnonymous();
            app.UseSwaggerUI(options =>
            {
                options.DocumentTitle = "LossLens API";
                options.SwaggerEndpoint("/openapi/v1.json", "LossLens v1");
                options.RoutePrefix = "swagger";
                options.EnablePersistAuthorization();
            });
        }

        // Behind a proxy or in a container there is no HTTPS port to redirect to, so it is on only in development
        // unless Https:Redirect says otherwise.
        if (app.Configuration.GetValue("Https:Redirect", app.Environment.IsDevelopment()))
        {
            app.UseHttpsRedirection();
        }

        app.UseCors(FrontendCorsPolicy);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
    }

    /// <summary>
    /// Fills an empty database with demo data and adds the hours that passed since the last start.
    /// Turn it off with Data:SeedMockData = false once real data arrives.
    /// </summary>
    private static void SeedMockData(WebApplication app, IServiceScope scope)
    {
        if (!app.Configuration.GetValue("Data:SeedMockData", true))
        {
            return;
        }

        var now = scope.ServiceProvider.GetRequiredService<PlantClock>().Now;
        var added = scope.ServiceProvider.GetRequiredService<MockDataSeeder>()
            .SeedAsync(DateOnly.FromDateTime(now), now.Hour).GetAwaiter().GetResult();

        app.Logger.LogInformation("Mock data: {Count} hourly records added", added);
    }

    /// <summary>Creates the demo users when a demo password is configured (Auth__DemoPassword in .env).</summary>
    private static void SeedDemoUsers(WebApplication app, IServiceScope scope)
    {
        var password = app.Configuration["Auth:DemoPassword"];
        if (string.IsNullOrEmpty(password))
        {
            return;
        }

        if (password.Length < 8)
            throw new InvalidOperationException("Auth:DemoPassword must have at least 8 characters.");

        var created = scope.ServiceProvider.GetRequiredService<DemoUserSeeder>()
            .SeedAsync(password).GetAwaiter().GetResult();

        app.Logger.LogInformation("Demo users created: {Count}", created);
    }
}
