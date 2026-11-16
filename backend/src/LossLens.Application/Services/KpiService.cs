using LossLens.Application.Calculations;
using LossLens.Application.Dtos;
using LossLens.Application.Security;
using LossLens.Application.Time;
using LossLens.Core.Abstractions;
using LossLens.Core.Config;
using LossLens.Core.Metrics;
using Microsoft.Extensions.Options;

namespace LossLens.Application.Services;

public class KpiService(
    IMetricsRepository metricsRepository,
    ScopeResolver scopeResolver,
    PlantClock plantClock,
    IOptions<ThresholdsOptions> thresholds,
    IOptions<MetricsOptions> metricsOptions)
{
    /// <summary>
    /// The four KPI cards for one day. Today is compared with the same hours of yesterday, so a
    /// half-finished day is not measured against a whole one. Other days are compared with the full previous day.
    /// </summary>
    public async Task<KpiSummaryDto> GetSummaryAsync(
        UserContext user, DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct = default)
    {
        var lineIds = scopeResolver.Resolve(user, lineId);

        // Last clock hour that counts. Same rule as the plan chart: the current hour is included.
        var now = plantClock.Now;
        var today = DateOnly.FromDateTime(now);
        var lastHour = date < today ? 23 : date == today ? now.Hour : -1;
        var previousDate = date.AddDays(-1);

        var stations = await metricsRepository.GetStationsInScopeAsync(lineIds, ct);
        var hourly = await metricsRepository.GetHourlyRecordsAsync(previousDate, date, lineIds, shiftId, ct);
        var tests = await metricsRepository.GetTestRecordsAsync(previousDate, date, lineIds, shiftId, ct);
        var entries = await metricsRepository.GetDefectEntriesAsync(previousDate, date, lineIds, null, shiftId, ct);
        var codes = await metricsRepository.GetDefectCodesAsync(ct);
        var codeNames = codes.ToDictionary(c => c.Code, c => c.Name);

        DaySnapshot Snapshot(DateOnly day)
        {
            var dayHourly = hourly.Where(r => r.Date == day && r.Hour <= lastHour).ToList();
            var dayTests = tests.Where(r => r.Date == day && r.Hour <= lastHour).ToList();
            var dayEntries = entries.Where(e => e.Date == day && e.Hour <= lastHour);

            return new DaySnapshot(
                PlanAttainmentCalculator.Calculate(dayHourly.Sum(r => r.PlannedUnits), dayHourly.Sum(r => r.GoodUnits)),
                LineYield.Rty(stations, dayTests),
                DefectReport.Build(dayEntries, stations, dayTests, codeNames, metricsOptions.Value.OpportunitiesPerUnit));
        }

        var current = Snapshot(date);
        var previous = Snapshot(previousDate);
        var limits = thresholds.Value;
        var top = current.Defects.Items.FirstOrDefault();

        return new KpiSummaryDto(
            date,
            new MetricDto(
                current.PlanAttainment,
                limits.PlanAttainment.Green,
                RagEvaluator.Evaluate(current.PlanAttainment, limits.PlanAttainment),
                Delta(current.PlanAttainment, previous.PlanAttainment)),
            new RtyMetricDto(
                current.Rty.Value,
                limits.LineFpy.Green,
                RagEvaluator.Evaluate(current.Rty.Value, limits.LineFpy),
                Delta(current.Rty.Value, previous.Rty.Value),
                current.Rty.Incomplete),
            new DpmoDto(current.Defects.Dpmo),
            top is null ? null : new TopDefectDto(top.Code, top.Name, top.Count));
    }

    private static double? Delta(double? current, double? previous) =>
        current is { } c && previous is { } p ? c - p : null;

    private sealed record DaySnapshot(double? PlanAttainment, RtyResult Rty, ParetoResponseDto Defects);
}
