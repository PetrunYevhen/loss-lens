using LossLens.Api.Configuration;
using LossLens.Core.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LossLens.Api.Tests;

/// <summary>Checks that the configuration sections used by Startup really bind to the option classes.</summary>
public class OptionsBindingTests
{
    private static ServiceProvider Build(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddMetricOptions(configuration);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void WithoutSections_DefaultsApply()
    {
        using var provider = Build([]);

        var thresholds = provider.GetRequiredService<IOptions<ThresholdsOptions>>().Value;
        var metrics = provider.GetRequiredService<IOptions<MetricsOptions>>().Value;

        Assert.Equal((95, 85), (thresholds.PlanAttainment.Green, thresholds.PlanAttainment.Amber));
        Assert.Equal(200, metrics.OpportunitiesPerUnit);
    }

    [Fact]
    public void Metrics_CanBeOverridden()
    {
        using var provider = Build(new() { ["Metrics:OpportunitiesPerUnit"] = "150" });

        Assert.Equal(150, provider.GetRequiredService<IOptions<MetricsOptions>>().Value.OpportunitiesPerUnit);
    }

    [Fact]
    public void Thresholds_PartialOverride_KeepsTheOtherDefaults()
    {
        using var provider = Build(new() { ["Thresholds:LineFpy:Green"] = "99" });

        var thresholds = provider.GetRequiredService<IOptions<ThresholdsOptions>>().Value;

        Assert.Equal((99, 88), (thresholds.LineFpy.Green, thresholds.LineFpy.Amber));
        Assert.Equal((95, 85), (thresholds.PlanAttainment.Green, thresholds.PlanAttainment.Amber));
    }

    [Fact]
    public void Thresholds_GreenBelowAmber_IsRejected()
    {
        using var provider = Build(new()
        {
            ["Thresholds:StationFpy:Green"] = "80",
            ["Thresholds:StationFpy:Amber"] = "90"
        });

        Assert.Throws<ArgumentException>(() => provider.GetRequiredService<IOptions<ThresholdsOptions>>().Value);
    }

    [Fact]
    public void Thresholds_CanBeOverridden()
    {
        using var provider = Build(new()
        {
            ["Thresholds:PlanAttainment:Green"] = "90",
            ["Thresholds:PlanAttainment:Amber"] = "80"
        });

        var plan = provider.GetRequiredService<IOptions<ThresholdsOptions>>().Value.PlanAttainment;

        Assert.Equal((90, 80), (plan.Green, plan.Amber));
    }
}
