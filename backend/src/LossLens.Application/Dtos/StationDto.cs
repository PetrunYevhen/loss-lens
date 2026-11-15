namespace LossLens.Application.Dtos;

/// <param name="Type">SPI, AOI, ICT or FCT.</param>
/// <param name="Order">1-based position along the line.</param>
public sealed record StationDto(Guid Id, string Name, string Type, int Order);
