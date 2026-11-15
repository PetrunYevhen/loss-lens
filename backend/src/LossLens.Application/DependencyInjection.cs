using LossLens.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LossLens.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the application services. They are scoped because they use the repository,
    /// which depends on the scoped database context.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LinesService>();
        services.AddScoped<KpiService>();
        services.AddScoped<PlanService>();
        services.AddScoped<FpyService>();
        services.AddScoped<DefectsService>();

        return services;
    }
}
