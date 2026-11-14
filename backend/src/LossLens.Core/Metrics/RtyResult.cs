namespace LossLens.Core.Metrics;

/// <param name="Value">Rolled Throughput Yield in percent, or null when it cannot be calculated.</param>
/// <param name="Incomplete">True when at least one station had no data and was skipped.</param>
public record RtyResult(double? Value, bool Incomplete);
