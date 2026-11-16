using LossLens.Application.Dtos;
using LossLens.Core.Metrics;
using LossLens.Core.Models;

namespace LossLens.Application.Calculations;

internal static class DefectReport
{
    /// <summary>Pareto items and DPMO from already filtered defect entries and test records.</summary>
    /// <param name="stations">Stations the figures are calculated for; the first one of each line gives the unit count.</param>
    /// <param name="codeNames">Defect code to display name. An unknown code is shown by its code.</param>
    public static ParetoResponseDto Build(
        IEnumerable<DefectEntry> entries,
        IReadOnlyCollection<Station> stations,
        IEnumerable<TestRecord> tests,
        IReadOnlyDictionary<string, string> codeNames,
        int opportunitiesPerUnit)
    {
        var entryList = entries.ToList();

        var items = DefectCalculator.Pareto(entryList.Select(e => (e.DefectCode, e.Count)))
            .Select(item => new ParetoItemDto(
                item.Code,
                codeNames.GetValueOrDefault(item.Code, item.Code),
                item.Count,
                item.Share,
                item.CumulativeShare))
            .ToList();

        var dpmo = DefectCalculator.Dpmo(
            entryList.Sum(e => e.Count),
            LineYield.UnitsEntering(stations, tests),
            opportunitiesPerUnit);

        return new ParetoResponseDto(items, dpmo);
    }
}
