using LossLens.Core.Models.Enums;

namespace LossLens.Application.Dtos;

/// <param name="Value">Null when the metric cannot be calculated (for example nothing planned).</param>
/// <param name="Target">The green threshold.</param>
public sealed record MetricDto(double? Value, double Target, RagStatus Status, double? DeltaVsPrevDay);
