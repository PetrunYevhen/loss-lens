using LossLens.Application.Dtos;
using LossLens.Application.Mapping;
using LossLens.Application.Security;
using LossLens.Core.Abstractions;

namespace LossLens.Application.Services;

public class LinesService
{
    private readonly IMetricsRepository _metricsRepository;
    public LinesService(IMetricsRepository metricsRepository)
    {
        _metricsRepository = metricsRepository;
    }

    public async Task<IReadOnlyList<LineDto>> GetLinesAsync(UserContext user, CancellationToken ct = default)
    {
        var lines = await _metricsRepository.GetLinesAsync(ct);
        var userLines = lines.Where(l => user.CanSee(l.Id));

        var stations = await _metricsRepository.GetStationsAsync(null, ct);
        var stationsByLine = stations.ToLookup(s => s.LineId);

        return userLines
            .Select(line => new LineDto(
                line.Id,
                line.Name,
                stationsByLine[line.Id]
                    .Select((station, index) => new StationDto(
                        station.Id,
                        station.Name,
                        station.Type.ToCode(),
                        index + 1))
                    .ToList()))
            .ToList();
    }
}
