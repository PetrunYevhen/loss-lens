using LossLens.Core.Abstractions;
using LossLens.Infrastructure.Mock;
using LossLens.Infrastructure.Persistence;
using LossLens.Infrastructure.Repositories;
using LossLens.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<DemoUserSeeder>();
        services.AddScoped<MockDataSeeder>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        return services;
    }
}
