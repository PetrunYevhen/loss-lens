namespace LossLens.Application.Dtos;

/// <param name="Hour">Hour of day, 0-23.</param>
/// <param name="Good">Null for hours that have not started yet.</param>
public sealed record PlanVsActualPointDto(int Hour, int Planned, int? Good);
