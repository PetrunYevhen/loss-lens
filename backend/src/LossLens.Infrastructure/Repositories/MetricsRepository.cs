using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Infrastructure.Repositories;

/// <summary>
/// PostgreSQL implementation of <see cref="IMetricsRepository"/>. Read-only, so nothing is tracked.
/// The models have no navigation properties, so line filters on station records go through a subquery.
/// </summary>
public sealed class MetricsRepository(LossLensDbContext context) : IMetricsRepository
{
    public async Task<IReadOnlyList<ProductionLine>> GetLinesAsync(CancellationToken ct = default) =>
        await context.ProductionLines.AsNoTracking().OrderBy(l => l.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Station>> GetStationsAsync(Guid? lineId = null, CancellationToken ct = default)
    {
        var query = context.Stations.AsNoTracking();
        if (lineId is not null)
        {
            query = query.Where(s => s.LineId == lineId);
        }

        // Type is stored as text, so the line order (SPI, AOI, ICT, FCT) is applied in memory.
        var stations = await query.ToListAsync(ct);
        return stations.OrderBy(s => s.LineId).ThenBy(s => s.Type).ToList();
    }

    public async Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken ct = default) =>
        await context.Shifts.AsNoTracking().OrderBy(s => s.StartTime).ToListAsync(ct);

    public async Task<IReadOnlyList<DefectCode>> GetDefectCodesAsync(CancellationToken ct = default) =>
        await context.DefectCodes.AsNoTracking().OrderBy(c => c.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<HourlyRecord>> GetHourlyRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default)
    {
        var query = context.HourlyRecords.AsNoTracking().Where(r => r.Date >= from && r.Date <= to);

        if (lineIds is not null)
        {
            var ids = lineIds.ToArray();
            query = query.Where(r => ids.Contains(r.LineId));
        }

        if (shiftId is not null)
        {
            query = query.Where(r => r.ShiftId == shiftId);
        }

        return await query.OrderBy(r => r.Date).ThenBy(r => r.Hour).ThenBy(r => r.LineId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TestRecord>> GetTestRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default)
    {
        var query = context.TestRecords.AsNoTracking().Where(r => r.Date >= from && r.Date <= to);

        if (lineIds is not null)
        {
            var stationIds = StationIdsOfLines(lineIds);
            query = query.Where(r => stationIds.Contains(r.StationId));
        }

        if (shiftId is not null)
        {
            var hours = await ShiftHoursAsync(shiftId.Value, ct);
            query = query.Where(r => hours.Contains(r.Hour));
        }

        return await query.OrderBy(r => r.Date).ThenBy(r => r.Hour).ThenBy(r => r.StationId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DefectEntry>> GetDefectEntriesAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? stationId = null,
        Guid? shiftId = null,
        CancellationToken ct = default)
    {
        var query = context.DefectEntries.AsNoTracking().Where(e => e.Date >= from && e.Date <= to);

        if (lineIds is not null)
        {
            var stationIds = StationIdsOfLines(lineIds);
            query = query.Where(e => stationIds.Contains(e.StationId));
        }

        if (stationId is not null)
        {
            query = query.Where(e => e.StationId == stationId);
        }

        if (shiftId is not null)
        {
            var hours = await ShiftHoursAsync(shiftId.Value, ct);
            query = query.Where(e => hours.Contains(e.Hour));
        }

        return await query.OrderBy(e => e.Date).ThenBy(e => e.Hour).ThenBy(e => e.DefectCode).ToListAsync(ct);
    }

    private IQueryable<Guid> StationIdsOfLines(IReadOnlyCollection<Guid> lineIds)
    {
        var ids = lineIds.ToArray();
        return context.Stations.Where(s => ids.Contains(s.LineId)).Select(s => s.Id);
    }

    /// <summary>
    /// Test and defect records have no shift column, so a shift is turned into its clock hours.
    /// An unknown shift matches nothing.
    /// </summary>
    private async Task<int[]> ShiftHoursAsync(Guid shiftId, CancellationToken ct)
    {
        var shift = await context.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == shiftId, ct);
        return shift is null ? [] : Enumerable.Range(0, 24).Where(shift.Contains).ToArray();
    }
}
