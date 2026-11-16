using LossLens.Application.Security;
using LossLens.Application.Services;
using LossLens.Application.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LossLens.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application services. They are scoped because they use the repository,
    /// which depends on the scoped database context.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="plantTimeZoneId">
    /// Time zone id of the plant (for example "Europe/Kyiv"). When null, the server's local zone is used,
    /// which is UTC in a container.
    /// </param>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? plantTimeZoneId = null)
    {
        var timeZone = plantTimeZoneId is null
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(plantTimeZoneId);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(sp => new PlantClock(sp.GetRequiredService<TimeProvider>(), timeZone));
        services.AddSingleton<ScopeResolver>();

        services.AddScoped<LinesService>();
        services.AddScoped<ConfigService>();
        services.AddScoped<KpiService>();
        services.AddScoped<PlanService>();
        services.AddScoped<FpyService>();
        services.AddScoped<DefectsService>();

        return services;
    }
}
