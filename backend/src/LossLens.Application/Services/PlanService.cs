using LossLens.Application.Dtos;
using LossLens.Application.Security;
using LossLens.Application.Time;
using LossLens.Core.Abstractions;

namespace LossLens.Application.Services;

public class PlanService
{
    private readonly IMetricsRepository _metricsRepository;
    private readonly ScopeResolver _scopeResolver;
    private readonly PlantClock _plantClock;

    public PlanService(IMetricsRepository metricsRepository, ScopeResolver scopeResolver, PlantClock plantClock)
    {
        _metricsRepository = metricsRepository;
        _scopeResolver = scopeResolver;
        _plantClock = plantClock;
    }

    public async Task<IReadOnlyList<PlanVsActualPointDto>> GetAsync(UserContext userContext,
        DateOnly date, Guid? lineId, Guid? shiftId, CancellationToken ct = default)
    {
        var lineIds = _scopeResolver.Resolve(userContext, lineId);
        var records = await _metricsRepository
            .GetHourlyRecordsAsync(date, date, lineIds, shiftId, ct);

        var now = _plantClock.Now;
        var today = DateOnly.FromDateTime(now);

        // An hour has started when its day is past, or it is today and the clock has reached it.
        // Stored values of future hours are ignored, the mock data may contain a whole day.
        bool HasStarted(int hour) => date < today || (date == today && hour <= now.Hour);

        return records
            .GroupBy(r => r.Hour)
            .OrderBy(g => g.Key)
            .Select(g => new PlanVsActualPointDto(
                g.Key,
                g.Sum(r => r.PlannedUnits),
                HasStarted(g.Key) ? g.Sum(r => r.GoodUnits) : null))
            .ToList();
    }
}
