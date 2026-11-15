namespace LossLens.Application.Dtos;

public sealed record LineDto(Guid Id, string Name, IReadOnlyList<StationDto> Stations);
