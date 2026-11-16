using LossLens.Application.Exceptions;
using LossLens.Core.Abstractions;

namespace LossLens.Application.Validation;

/// <summary>
/// Checks that the ids of a request point to something that exists, so a typo is a 404 and not an empty
/// answer that looks like "no data". Run after the scope check: a user outside the scope gets 403 first,
/// which does not reveal whether a line exists.
/// </summary>
internal static class ReferenceChecks
{
    /// <exception cref="NotFoundException">A line, shift or station id is given but unknown.</exception>
    public static async Task EnsureExistAsync(
        this IMetricsRepository repository, Guid? lineId, Guid? shiftId, Guid? stationId, CancellationToken ct)
    {
        if (lineId is { } line && (await repository.GetLinesAsync(ct)).All(l => l.Id != line))
            throw new NotFoundException($"Line {line} does not exist.");

        if (shiftId is { } shift && (await repository.GetShiftsAsync(ct)).All(s => s.Id != shift))
            throw new NotFoundException($"Shift {shift} does not exist.");

        if (stationId is { } station && (await repository.GetStationsAsync(null, ct)).All(s => s.Id != station))
            throw new NotFoundException($"Station {station} does not exist.");
    }
}
