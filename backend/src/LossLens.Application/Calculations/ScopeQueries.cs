using LossLens.Core.Abstractions;
using LossLens.Core.Models;

namespace LossLens.Application.Calculations;

internal static class ScopeQueries
{
    /// <summary>
    /// Stations of the lines in scope, ordered by line and then by position on the line.
    /// A null <paramref name="lineIds"/> means all lines.
    /// </summary>
    public static async Task<IReadOnlyList<Station>> GetStationsInScopeAsync(
        this IMetricsRepository repository,
        IReadOnlyCollection<Guid>? lineIds,
        CancellationToken ct)
    {
        var stations = await repository.GetStationsAsync(null, ct);

        return lineIds is null
            ? stations
            : stations.Where(s => lineIds.Contains(s.LineId)).ToList();
    }
}
