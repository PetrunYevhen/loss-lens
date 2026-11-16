using LossLens.Application.Calculations;
using LossLens.Application.Dtos;
using LossLens.Application.Security;
using LossLens.Application.Validation;
using LossLens.Core.Abstractions;
using LossLens.Core.Config;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Services;

public class DefectsService(
    IMetricsRepository metricsRepository,
    ScopeResolver scopeResolver,
    IOptions<MetricsOptions> metricsOptions)
{
    /// <summary>
    /// Pareto of defect causes and DPMO for a date range. DPMO divides the defects by the units that
    /// entered the line (tested at the first station) times the opportunities per unit. With a station
    /// filter, the units are those tested at that station.
    /// </summary>
    public async Task<ParetoResponseDto> GetParetoAsync(
        UserContext user,
        DateOnly from,
        DateOnly to,
        Guid? lineId,
        Guid? stationId,
        Guid? shiftId,
        CancellationToken ct = default)
    {
        RequestValidation.EnsureDateRange(from, to);

        var lineIds = scopeResolver.Resolve(user, lineId);
        var stations = await metricsRepository.GetStationsInScopeAsync(lineIds, ct);
        if (stationId is { } id)
        {
            stations = stations.Where(s => s.Id == id).ToList();
        }

        var entries = await metricsRepository.GetDefectEntriesAsync(from, to, lineIds, stationId, shiftId, ct);
        var tests = await metricsRepository.GetTestRecordsAsync(from, to, lineIds, shiftId, ct);
        var codes = await metricsRepository.GetDefectCodesAsync(ct);

        return DefectReport.Build(
            entries,
            stations,
            tests,
            codes.ToDictionary(c => c.Code, c => c.Name),
            metricsOptions.Value.OpportunitiesPerUnit);
    }
}
