using LossLens.Application.Services;
using LossLens.Core.Config;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Tests;

public class ConfigServiceTests
{
    [Fact]
    public void DefaultOptions_GiveDefaultThresholds()
    {
        var service = new ConfigService(Options.Create(new ThresholdsOptions()));

        var dto = service.GetThresholdIds();

        Assert.Equal((95, 85), (dto.PlanAttainment.Green, dto.PlanAttainment.Amber));
        Assert.Equal((92, 88), (dto.LineFpy.Green, dto.LineFpy.Amber));
        Assert.Equal((97, 94), (dto.StationFpy.Green, dto.StationFpy.Amber));
    }

    [Fact]
    public void CustomOptions_AreMappedToTheirOwnFields()
    {
        var options = new ThresholdsOptions
        {
            PlanAttainment = new Threshold(90, 80),
            LineFpy = new Threshold(70, 60),
            StationFpy = new Threshold(99, 98)
        };

        var dto = new ConfigService(Options.Create(options)).GetThresholdIds();

        Assert.Equal((90, 80), (dto.PlanAttainment.Green, dto.PlanAttainment.Amber));
        Assert.Equal((70, 60), (dto.LineFpy.Green, dto.LineFpy.Amber));
        Assert.Equal((99, 98), (dto.StationFpy.Green, dto.StationFpy.Amber));
    }
}
