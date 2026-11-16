using LossLens.Application.Dtos;
using LossLens.Core.Config;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Services;

public class ConfigService
{
    private readonly IOptions<ThresholdsOptions> _options;

    public ConfigService(IOptions<ThresholdsOptions> options)
    {
        _options = options;
    }

    public ThresholdsDto GetThresholdIds()
    {
        var thresholdsOptions = _options.Value;
        return new ThresholdsDto(
            Map(thresholdsOptions.PlanAttainment),
            Map(thresholdsOptions.LineFpy),
            Map(thresholdsOptions.StationFpy));
    }
    
    private static ThresholdDto Map(Threshold t) 
        => new(t.Green, t.Amber);
}
