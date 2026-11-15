namespace LossLens.Application.Dtos;

public sealed record ParetoResponseDto(IReadOnlyList<ParetoItemDto> Items, double? Dpmo);
