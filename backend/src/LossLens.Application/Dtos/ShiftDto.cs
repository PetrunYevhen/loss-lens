namespace LossLens.Application.Dtos;

/// <param name="Start">Start of the shift as HH:mm.</param>
/// <param name="End">End of the shift as HH:mm. It is earlier than the start for a shift that crosses midnight.</param>
public sealed record ShiftDto(Guid Id, string Name, string Start, string End);
