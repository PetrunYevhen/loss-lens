using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Infrastructure.Mock;

/// <summary>
/// Deterministic production data for the demo: the same hour of the same line always gets the same numbers,
/// whenever and in whatever order the data is generated. Nothing here touches the database.
/// </summary>
/// <remarks>
/// Ranges follow REQUIREMENTS FR-DAT: 80-120 planned units per line and hour, station FPY 94-99.5%,
/// a few defect causes that dominate, and two planted anomalies: shift C has a lower FPY on every line,
/// and Line 3 produces almost nothing in hour 10 once a week.
/// </remarks>
public sealed class MockDataGenerator(int seed = 42)
{
    private const int ShiftCPenaltyPercentPoints = 3;

    /// <summary>Line 3 stalls in hour 10 on one weekday a week (the day number modulo 7), so it shows up in any month.</summary>
    private const int NearZeroWeekday = 3;

    private static readonly string[] LineNames = ["Line 1", "Line 2", "Line 3"];

    private static readonly StationType[] StationTypes = [StationType.Spi, StationType.Aoi, StationType.Ict, StationType.Fct];

    /// <summary>Base first pass yield (percent) per line and station type, in line order.</summary>
    private static readonly double[][] BaseFpy =
    [
        [99.4, 99.0, 98.4, 99.5], // Line 1
        [98.6, 97.6, 96.4, 98.8], // Line 2
        [97.8, 96.4, 95.2, 98.2]  // Line 3
    ];

    private static readonly DefectCode[] Codes =
    [
        new("SB", "Solder bridge", "Soldering"),
        new("IS", "Insufficient solder", "Soldering"),
        new("MC", "Missing component", "Placement"),
        new("TS", "Tombstone", "Placement"),
        new("WP", "Wrong polarity", "Placement"),
        new("MA", "Component misalignment", "Placement"),
        new("OC", "Open circuit", "Electrical"),
        new("SH", "Short circuit", "Electrical"),
        new("FF", "Functional failure", "Functional"),
        new("FW", "Firmware error", "Functional")
    ];

    /// <summary>Share of the failed units of a station type that each defect cause takes. The shares add up to 1.</summary>
    private static readonly Dictionary<StationType, (string Code, double Weight)[]> CauseWeights = new()
    {
        [StationType.Spi] = [("SB", 0.55), ("IS", 0.45)],
        [StationType.Aoi] = [("MC", 0.30), ("TS", 0.25), ("WP", 0.20), ("MA", 0.15), ("SB", 0.10)],
        [StationType.Ict] = [("OC", 0.45), ("SH", 0.35), ("WP", 0.20)],
        [StationType.Fct] = [("FF", 0.60), ("FW", 0.40)]
    };

    /// <summary>Lines, stations, shifts and defect codes. Entities get new ids, so create it once per database.</summary>
    public MockCatalog CreateCatalog()
    {
        var lines = LineNames.Select(name => new ProductionLine(name)).ToList();
        var stations = lines
            .SelectMany((line, index) => StationTypes.Select(type =>
                new Station(line.Id, $"{type.ToString().ToUpperInvariant()}-{index + 1}", type)))
            .ToList();
        var shifts = new List<Shift>
        {
            new("A", new TimeOnly(6, 0), new TimeOnly(14, 0)),
            new("B", new TimeOnly(14, 0), new TimeOnly(22, 0)),
            new("C", new TimeOnly(22, 0), new TimeOnly(6, 0))
        };

        return new MockCatalog(lines, stations, shifts, Codes.ToList());
    }

    /// <summary>Hourly, test and defect records for each of the given clock hours.</summary>
    public MockRecords CreateRecords(MockCatalog catalog, IEnumerable<(DateOnly Date, int Hour)> slots)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(slots);

        var lines = catalog.Lines.OrderBy(l => l.Name, StringComparer.Ordinal).ToList();
        var shifts = catalog.Shifts.ToList();
        var stationsByLine = catalog.Stations
            .GroupBy(s => s.LineId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.Type).ToList());

        var records = new MockRecords();

        foreach (var (date, hour) in slots)
        {
            var shift = shifts.First(s => s.Contains(hour));

            for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var line = lines[lineIndex];
                var random = new Random(SlotSeed(date, hour, lineIndex));

                var planned = 80 + random.Next(41);
                var nearZero = lineIndex == 2 && hour == 10 && date.DayNumber % 7 == NearZeroWeekday;

                var enteringUnits = 0;
                var passRate = 1.0;

                foreach (var station in stationsByLine[line.Id])
                {
                    var typeIndex = Array.IndexOf(StationTypes, station.Type);
                    var tested = nearZero ? 2 + random.Next(5) : Math.Max(0, planned + random.Next(-3, 4));
                    var fpy = FirstPassYield(lineIndex, typeIndex, shift, random);
                    var firstPass = Math.Clamp((int)Math.Round(tested * fpy / 100), 0, tested);

                    if (station.Type == StationType.Spi)
                    {
                        enteringUnits = tested;
                    }

                    passRate *= tested == 0 ? 1 : firstPass / (double)tested;
                    records.Tests.Add(new TestRecord(date, hour, station.Id, tested, firstPass));

                    foreach (var (code, count) in SplitDefects(station.Type, tested - firstPass, random))
                    {
                        records.Defects.Add(new DefectEntry(date, hour, station.Id, code, count));
                    }
                }

                records.Hourly.Add(new HourlyRecord(date, hour, line.Id, shift.Id, planned, (int)Math.Round(enteringUnits * passRate)));
            }
        }

        return records;
    }

    private int SlotSeed(DateOnly date, int hour, int lineIndex) =>
        unchecked(((date.DayNumber * 24 + hour) * 7919) + (lineIndex * 104729) + seed);

    private static double FirstPassYield(int lineIndex, int typeIndex, Shift shift, Random random)
    {
        var baseYield = BaseFpy[Math.Min(lineIndex, BaseFpy.Length - 1)][typeIndex];
        var noise = (random.NextDouble() - 0.5) * 2.0;
        var penalty = shift.Name == "C" ? ShiftCPenaltyPercentPoints : 0;

        return Math.Clamp(baseYield + noise - penalty, 90.0, 99.9);
    }

    /// <summary>Spreads the failed units over the defect causes of a station type. The counts add up to the failures.</summary>
    private static IEnumerable<(string Code, int Count)> SplitDefects(StationType type, int failed, Random random)
    {
        if (failed <= 0)
        {
            yield break;
        }

        var causes = CauseWeights[type];
        var weights = causes.Select(c => c.Weight * (0.85 + random.NextDouble() * 0.3)).ToArray();
        var total = weights.Sum();
        var shares = weights.Select(w => w / total * failed).ToArray();
        var counts = shares.Select(s => (int)Math.Floor(s)).ToArray();

        // Largest remainder: hand the leftover units to the causes that lost the most when rounding down.
        var left = failed - counts.Sum();
        foreach (var index in Enumerable.Range(0, causes.Length).OrderByDescending(i => shares[i] - counts[i]).Take(left))
        {
            counts[index]++;
        }

        for (var i = 0; i < causes.Length; i++)
        {
            if (counts[i] > 0)
            {
                yield return (causes[i].Code, counts[i]);
            }
        }
    }
}

public sealed record MockCatalog(
    IReadOnlyList<ProductionLine> Lines,
    IReadOnlyList<Station> Stations,
    IReadOnlyList<Shift> Shifts,
    IReadOnlyList<DefectCode> DefectCodes);

public sealed class MockRecords
{
    public List<HourlyRecord> Hourly { get; } = [];
    public List<TestRecord> Tests { get; } = [];
    public List<DefectEntry> Defects { get; } = [];
}
