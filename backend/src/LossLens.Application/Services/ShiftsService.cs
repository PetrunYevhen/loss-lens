using LossLens.Application.Dtos;
using LossLens.Core.Abstractions;

namespace LossLens.Application.Services;

public class ShiftsService(IMetricsRepository metricsRepository)
{
    /// <summary>All shifts ordered by start time. The same for every role.</summary>
    public async Task<IReadOnlyList<ShiftDto>> GetShiftsAsync(CancellationToken ct = default)
    {
        var shifts = await metricsRepository.GetShiftsAsync(ct);

        return shifts
            .Select(s => new ShiftDto(s.Id, s.Name, s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm")))
            .ToList();
    }
}
