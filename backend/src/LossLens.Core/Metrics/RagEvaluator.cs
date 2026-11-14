using LossLens.Core.Config;
using LossLens.Core.Models.Enums;

namespace LossLens.Core.Metrics;

public static class RagEvaluator
{
    /// <summary>
    /// Red / Amber / Green status for a metric where a higher value is better.
    /// A null value has no status (Unknown).
    /// </summary>
    public static RagStatus Evaluate(double? value, Threshold threshold)
    {
        ArgumentNullException.ThrowIfNull(threshold);

        return value switch
        {
            null => RagStatus.Unknown,
            var v when v >= threshold.Green => RagStatus.Green,
            var v when v >= threshold.Amber => RagStatus.Amber,
            _ => RagStatus.Red
        };
    }
}
