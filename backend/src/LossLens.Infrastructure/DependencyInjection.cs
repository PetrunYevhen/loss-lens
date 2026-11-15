using LossLens.Core.Abstractions;
using LossLens.Infrastructure.Persistence;
using LossLens.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LossLens.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the database context (PostgreSQL). The connection string comes from the caller:
    /// keep the password out of the repository (user secrets or the ConnectionStrings__Default variable).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<LossLensDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IMetricsRepository, MetricsRepository>();

        return services;
    }
}
