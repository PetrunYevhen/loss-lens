namespace LossLens.Core.Models.Enums;

/// <summary>Red / Amber / Green status of a metric against its thresholds.</summary>
public enum RagStatus
{
    /// <summary>Metric value is null, so no status can be computed (shown grey).</summary>
    Unknown,

    Green,
    Amber,
    Red
}
