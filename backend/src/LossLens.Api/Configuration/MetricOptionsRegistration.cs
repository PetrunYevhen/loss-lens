using LossLens.Core.Config;

namespace LossLens.Api.Configuration;

public static class MetricOptionsRegistration
{
    /// <summary>
    /// Registers <see cref="ThresholdsOptions"/> (section "Thresholds") and <see cref="MetricsOptions"/> (section "Metrics").
    /// A missing value keeps the default of the option class.
    /// </summary>
    /// <remarks>
    /// Thresholds are read by hand: <see cref="Threshold"/> has read-only properties and validates them in its
    /// constructor, so the configuration binder cannot override the defaults. An invalid pair (green below amber)
    /// stops the application at start.
    /// </remarks>
    public static IServiceCollection AddMetricOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MetricsOptions>(configuration.GetSection("Metrics"));

        services.AddOptions<ThresholdsOptions>()
            .Configure(options =>
            {
                var section = configuration.GetSection("Thresholds");
                options.PlanAttainment = Read(section.GetSection(nameof(ThresholdsOptions.PlanAttainment)), options.PlanAttainment);
                options.LineFpy = Read(section.GetSection(nameof(ThresholdsOptions.LineFpy)), options.LineFpy);
                options.StationFpy = Read(section.GetSection(nameof(ThresholdsOptions.StationFpy)), options.StationFpy);
            })
            .ValidateOnStart();

        return services;
    }

    private static Threshold Read(IConfigurationSection section, Threshold fallback) =>
        new(section.GetValue("Green", fallback.Green), section.GetValue("Amber", fallback.Amber));
}
