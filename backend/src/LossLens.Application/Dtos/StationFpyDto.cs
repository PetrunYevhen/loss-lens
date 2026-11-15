using LossLens.Core.Models.Enums;

namespace LossLens.Application.Dtos;

/// <param name="StationId">A station id, or the station type when several lines are combined.</param>
public sealed record StationFpyDto(
    string StationId,
    string Name,
    int Tested,
    int FirstPass,
    double? Fpy,
    RagStatus Status);
