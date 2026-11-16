using LossLens.Core.Models.Enums;

namespace LossLens.Application.Dtos;

/// <param name="AccessToken">Sent back as <c>Authorization: Bearer</c> on every API call.</param>
/// <param name="LineIds">Lines the user may see. Empty for a manager.</param>
public sealed record LoginResultDto(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserRole Role,
    IReadOnlyList<Guid> LineIds);
