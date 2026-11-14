namespace LossLens.Core.Models.Enums;

/// <summary>Inspection or test stage on an SMT line.</summary>
public enum StationType
{
    /// <summary>Solder paste inspection.</summary>
    Spi,

    /// <summary>Automated optical inspection.</summary>
    Aoi,

    /// <summary>In-circuit test.</summary>
    Ict,

    /// <summary>Functional test.</summary>
    Fct
}
