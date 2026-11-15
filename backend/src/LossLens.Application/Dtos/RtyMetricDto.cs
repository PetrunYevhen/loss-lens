using LossLens.Core.Models.Enums;

namespace LossLens.Application.Dtos;

/// <param name="Incomplete">True when some stations had no data and were skipped.</param>
public sealed record RtyMetricDto(
    double? Value,
    double Target,
    RagStatus Status,
    double? DeltaVsPrevDay,
    bool Incomplete);
