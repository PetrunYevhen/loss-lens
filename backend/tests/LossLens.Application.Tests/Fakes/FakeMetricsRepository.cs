using LossLens.Core.Abstractions;
using LossLens.Core.Models;

namespace LossLens.Application.Tests.Fakes;

/// <summary>
/// In-memory <see cref="IMetricsRepository"/> for service tests. Fill the public lists, then pass the
/// fake to a service. Filtering and ordering follow the PostgreSQL repository, so services see the same
/// data shape in tests and in production.
/// </summary>
public sealed class FakeMetricsRepository : IMetricsRepository
{
    public List<ProductionLine> Lines { get; } = [];
    public List<Station> Stations { get; } = [];
    public List<Shift> Shifts { get; } = [];
    public List<DefectCode> DefectCodes { get; } = [];
    public List<HourlyRecord> HourlyRecords { get; } = [];
    public List<TestRecord> TestRecords { get; } = [];
    public List<DefectEntry> DefectEntries { get; } = [];

    public Task<IReadOnlyList<ProductionLine>> GetLinesAsync(CancellationToken ct = default) =>
        Result(Lines.OrderBy(l => l.Name));

    public Task<IReadOnlyList<Station>> GetStationsAsync(Guid? lineId = null, CancellationToken ct = default) =>
        Result(Stations
            .Where(s => lineId is null || s.LineId == lineId)
            .OrderBy(s => s.LineId)
            .ThenBy(s => s.Type));

    public Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken ct = default) =>
        Result(Shifts.OrderBy(s => s.StartTime));

    public Task<IReadOnlyList<DefectCode>> GetDefectCodesAsync(CancellationToken ct = default) =>
        Result(DefectCodes.OrderBy(c => c.Code));

    public Task<IReadOnlyList<HourlyRecord>> GetHourlyRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default) =>
        Result(HourlyRecords
            .Where(r => r.Date >= from && r.Date <= to)
            .Where(r => lineIds is null || lineIds.Contains(r.LineId))
            .Where(r => shiftId is null || r.ShiftId == shiftId)
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Hour)
            .ThenBy(r => r.LineId));

    public Task<IReadOnlyList<TestRecord>> GetTestRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default)
    {
        var stationIds = StationIdsOfLines(lineIds);
        var hours = ShiftHours(shiftId);

        return Result(TestRecords
            .Where(r => r.Date >= from && r.Date <= to)
            .Where(r => stationIds is null || stationIds.Contains(r.StationId))
            .Where(r => hours is null || hours.Contains(r.Hour))
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Hour)
            .ThenBy(r => r.StationId));
    }

    public Task<IReadOnlyList<DefectEntry>> GetDefectEntriesAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? stationId = null,
        Guid? shiftId = null,
        CancellationToken ct = default)
    {
        var stationIds = StationIdsOfLines(lineIds);
        var hours = ShiftHours(shiftId);

        return Result(DefectEntries
            .Where(e => e.Date >= from && e.Date <= to)
            .Where(e => stationIds is null || stationIds.Contains(e.StationId))
            .Where(e => stationId is null || e.StationId == stationId)
            .Where(e => hours is null || hours.Contains(e.Hour))
            .OrderBy(e => e.Date)
            .ThenBy(e => e.Hour)
            .ThenBy(e => e.DefectCode));
    }

    private HashSet<Guid>? StationIdsOfLines(IReadOnlyCollection<Guid>? lineIds) =>
        lineIds is null
            ? null
            : Stations.Where(s => lineIds.Contains(s.LineId)).Select(s => s.Id).ToHashSet();

    /// <summary>Test and defect records have no shift column; an unknown shift matches nothing.</summary>
    private HashSet<int>? ShiftHours(Guid? shiftId)
    {
        if (shiftId is null)
        {
            return null;
        }

        var shift = Shifts.FirstOrDefault(s => s.Id == shiftId);
        return shift is null
            ? []
            : Enumerable.Range(0, 24).Where(shift.Contains).ToHashSet();
    }

    private static Task<IReadOnlyList<T>> Result<T>(IEnumerable<T> items) =>
        Task.FromResult<IReadOnlyList<T>>(items.ToList());
}
