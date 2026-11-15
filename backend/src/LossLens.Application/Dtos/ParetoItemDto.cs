namespace LossLens.Application.Dtos;

/// <param name="Share">Percent of all defects, 0-100.</param>
/// <param name="CumulativeShare">Percent of all defects up to and including this item, 0-100.</param>
public sealed record ParetoItemDto(string Code, string Name, int Count, double Share, double CumulativeShare);
