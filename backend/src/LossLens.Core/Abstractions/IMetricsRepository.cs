using LossLens.Core.Models;

namespace LossLens.Core.Abstractions;

/// <summary>
/// Single read access point for production data. A real data source can replace the PostgreSQL
/// implementation without changing callers. Date ranges are inclusive.
/// A null filter means "all"; a <paramref name="lineIds"/> collection limits the result to those lines
/// (used to enforce the role scope).
/// </summary>
public interface IMetricsRepository
{
    Task<IReadOnlyList<ProductionLine>> GetLinesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Station>> GetStationsAsync(Guid? lineId = null, CancellationToken ct = default);

    Task<IReadOnlyList<Shift>> GetShiftsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DefectCode>> GetDefectCodesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<HourlyRecord>> GetHourlyRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<TestRecord>> GetTestRecordsAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? shiftId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<DefectEntry>> GetDefectEntriesAsync(
        DateOnly from,
        DateOnly to,
        IReadOnlyCollection<Guid>? lineIds = null,
        Guid? stationId = null,
        Guid? shiftId = null,
        CancellationToken ct = default);
}
