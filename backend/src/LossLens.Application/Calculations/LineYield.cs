using LossLens.Core.Metrics;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Calculations;

/// <summary>Counts and First Pass Yield of one station type, summed over every line in scope.</summary>
internal readonly record struct TypeYield(StationType Type, int Tested, int FirstPass, double? Fpy);

/// <summary>
/// Yield figures shared by several services. Station types are summed across lines first and the
/// percentages are calculated from the sums, so several lines combine correctly (never average percentages).
/// </summary>
internal static class LineYield
{
    /// <summary>
    /// One row per station type that exists in <paramref name="stations"/>, in line order (SPI to FCT).
    /// A type without test records has no FPY.
    /// </summary>
    public static IReadOnlyList<TypeYield> ByType(IReadOnlyCollection<Station> stations, IEnumerable<TestRecord> tests)
    {
        var typeOf = stations.ToDictionary(s => s.Id, s => s.Type);

        var sums = tests
            .Where(t => typeOf.ContainsKey(t.StationId))
            .GroupBy(t => typeOf[t.StationId])
            .ToDictionary(g => g.Key, g => (Tested: g.Sum(t => t.TestedUnits), FirstPass: g.Sum(t => t.FirstPassUnits)));

        return stations
            .Select(s => s.Type)
            .Distinct()
            .Order()
            .Select(type =>
            {
                sums.TryGetValue(type, out var sum);
                return new TypeYield(type, sum.Tested, sum.FirstPass, FpyCalculator.StationFpy(sum.Tested, sum.FirstPass));
            })
            .ToList();
    }

    /// <summary>Rolled Throughput Yield of the stations in scope.</summary>
    public static RtyResult Rty(IReadOnlyCollection<Station> stations, IEnumerable<TestRecord> tests) =>
        FpyCalculator.LineRty(ByType(stations, tests).Select(row => row.Fpy));

    /// <summary>
    /// Units that entered the line: units tested at the first station of each line.
    /// This is the unit count behind DPMO.
    /// </summary>
    public static int UnitsEntering(IReadOnlyCollection<Station> stations, IEnumerable<TestRecord> tests)
    {
        var firstStationIds = stations
            .GroupBy(s => s.LineId)
            .Select(g => g.OrderBy(s => s.Type).First().Id)
            .ToHashSet();

        return tests.Where(t => firstStationIds.Contains(t.StationId)).Sum(t => t.TestedUnits);
    }
}
