using LossLens.Core.Models.Enums;

namespace LossLens.Application.Mapping;

public static class StationTypeExtensions
{
    /// <summary>The upper-case code used in the API: SPI, AOI, ICT or FCT.</summary>
    public static string ToCode(this StationType type) => type.ToString().ToUpperInvariant();
}
