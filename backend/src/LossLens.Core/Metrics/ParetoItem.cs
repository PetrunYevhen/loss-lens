namespace LossLens.Core.Metrics;

/// <param name="Share">Share of all defects in percent.</param>
/// <param name="CumulativeShare">Share of this item plus all more frequent ones, in percent.</param>
public record ParetoItem(string Code, int Count, double Share, double CumulativeShare);
