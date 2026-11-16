using System.Text.Json;
using System.Text.Json.Serialization;
using LossLens.Api.Auth;
using LossLens.Api.Configuration;
using LossLens.Api.Errors;
using LossLens.Api.Health;
using LossLens.Application;
using LossLens.Application.Exceptions;
using LossLens.Infrastructure;
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

        services.AddOpenApi();
        services.AddProblemDetails();
        // services.AddExceptionHandler<ApplicationExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContextProvider, DemoUserContextProvider>();

        services.AddMetricOptions(_configuration);

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
        }

        // Client mistakes (400, 403) are answered, not logged as errors with a stack trace.
        // app.UseExceptionHandler(new ExceptionHandlerOptions
        // {
        //     SuppressDiagnosticsCallback = context => context.Exception is ValidationException or ForbiddenException
        // });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseCors(FrontendCorsPolicy);
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");
    }
}
