using LossLens.Application.Calculations;
using LossLens.Application.Dtos;
using LossLens.Application.Mapping;
using LossLens.Application.Security;
using LossLens.Application.Time;
using LossLens.Application.Validation;
using LossLens.Core.Abstractions;
using LossLens.Core.Config;
using LossLens.Core.Metrics;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Services;

public class FpyService(
    IMetricsRepository metricsRepository,
    ScopeResolver scopeResolver,
    IOptions<ThresholdsOptions> thresholds,
    PlantClock plantClock)
{
    /// <summary>
    /// Daily Rolled Throughput Yield for a date range, up to today. Every day of the range is returned,
    /// a day without test records has a null RTY. Several lines are combined by station type.
    /// </summary>
    public async Task<IReadOnlyList<FpyTrendPointDto>> GetTrendAsync(
        UserContext user, DateOnly from, DateOnly to, Guid? lineId, CancellationToken ct = default)
    {
        RequestValidation.EnsureDateRange(from, to);

        // The future has no data. A range that lies entirely ahead of today is empty.
        var today = plantClock.Today;
        if (to > today)
        {
            to = today;
        }

        if (from > to)
        {
            return [];
        }

        var lineIds = scopeResolver.Resolve(user, lineId);
        var stations = await metricsRepository.GetStationsInScopeAsync(lineIds, ct);
        var tests = await metricsRepository.GetTestRecordsAsync(from, to, lineIds, null, ct);
        var testsByDate = tests.ToLookup(t => t.Date);

        var points = new List<FpyTrendPointDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            points.Add(new FpyTrendPointDto(day, LineYield.Rty(stations, testsByDate[day]).Value));
        }

        return points;
    }

    /// <summary>
    /// First Pass Yield per station for one day. When the scope is a single line, every station of the
    /// line is returned; otherwise stations are combined by type (SPI, AOI, ICT, FCT).
    /// </summary>
    public async Task<IReadOnlyList<StationFpyDto>> GetStationsAsync(
        UserContext user, DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct = default)
    {
        var lineIds = scopeResolver.Resolve(user, lineId);
        var stations = await metricsRepository.GetStationsInScopeAsync(lineIds, ct);
        var tests = await metricsRepository.GetTestRecordsAsync(date, date, lineIds, shiftId, ct);
        var threshold = thresholds.Value.StationFpy;

        var singleLine = stations.Select(s => s.LineId).Distinct().Count() == 1;
        if (singleLine)
        {
            var sums = tests
                .GroupBy(t => t.StationId)
                .ToDictionary(g => g.Key, g => (Tested: g.Sum(t => t.TestedUnits), FirstPass: g.Sum(t => t.FirstPassUnits)));

            return stations
                .Select(station =>
                {
                    sums.TryGetValue(station.Id, out var sum);
                    var fpy = FpyCalculator.StationFpy(sum.Tested, sum.FirstPass);
                    return new StationFpyDto(
                        station.Id.ToString(),
                        station.Name,
                        sum.Tested,
                        sum.FirstPass,
                        fpy,
                        RagEvaluator.Evaluate(fpy, threshold));
                })
                .ToList();
        }

        return LineYield.ByType(stations, tests)
            .Select(row => new StationFpyDto(
                row.Type.ToCode(),
                row.Type.ToCode(),
                row.Tested,
                row.FirstPass,
                row.Fpy,
                RagEvaluator.Evaluate(row.Fpy, threshold)))
            .ToList();
    }
}
