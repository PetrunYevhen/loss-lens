using LossLens.Core.Config;

namespace LossLens.Core.Tests;

public class ThresholdTests
{
    [Fact]
    public void Constructor_StoresValues()
    {
        var threshold = new Threshold(95, 85);

        Assert.Equal(95, threshold.Green);
        Assert.Equal(85, threshold.Amber);
    }

    [Theory]
    [InlineData(95, 85)]
    [InlineData(90, 90)]
    public void Constructor_Accepts_GreenNotLessThanAmber(double green, double amber)
    {
        var threshold = new Threshold(green, amber);

        Assert.Equal(green, threshold.Green);
        Assert.Equal(amber, threshold.Amber);
    }

    [Theory]
    [InlineData(85, 95)]
    [InlineData(90, 91)]
    public void Constructor_Throws_WhenGreenLessThanAmber(double green, double amber)
    {
        Assert.Throws<ArgumentException>(() => new Threshold(green, amber));
    }

    [Fact]
    public void ThresholdsOptions_HasDefaultsFromRequirements()
    {
        var options = new ThresholdsOptions();

        Assert.Equal(new Threshold(95, 85), options.PlanAttainment);
        Assert.Equal(new Threshold(92, 88), options.LineFpy);
        Assert.Equal(new Threshold(97, 94), options.StationFpy);
    }

    [Fact]
    public void MetricsOptions_DefaultsToTwoHundredOpportunitiesPerUnit()
    {
        Assert.Equal(200, new MetricsOptions().OpportunitiesPerUnit);
    }
}
