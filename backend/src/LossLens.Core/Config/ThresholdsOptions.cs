namespace LossLens.Core.Config;

public class ThresholdsOptions
{
    public Threshold PlanAttainment { get; set; } = new(95, 85);
    public Threshold LineFpy { get; set; } = new(92, 88);
    public Threshold StationFpy { get; set; } = new(97, 94);
}