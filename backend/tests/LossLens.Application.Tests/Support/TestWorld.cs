using LossLens.Application.Security;
using LossLens.Application.Tests.Fakes;
using LossLens.Application.Time;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;

namespace LossLens.Application.Tests.Support;

/// <summary>
/// Two production lines with the four station types each, one day shift (06:00-14:00) and a fake
/// repository holding them. Tests add only the records they care about.
/// </summary>
public sealed class TestWorld
{
    public static readonly DateOnly Today = new(2026, 11, 16);
    public static readonly DateOnly Yesterday = Today.AddDays(-1);

    public static readonly StationType[] Types = [StationType.Spi, StationType.Aoi, StationType.Ict, StationType.Fct];

    private readonly Dictionary<(Guid LineId, StationType Type), Station> _stations = [];

    public FakeMetricsRepository Repo { get; } = new();
    public ProductionLine Line1 { get; } = new("Line 1");
    public ProductionLine Line2 { get; } = new("Line 2");
    public Shift DayShift { get; } = new("Day", new TimeOnly(6, 0), new TimeOnly(14, 0));
    public Shift NightShift { get; } = new("Night", new TimeOnly(14, 0), new TimeOnly(22, 0));

    public TestWorld()
    {
        Repo.Shifts.Add(DayShift);
        Repo.Shifts.Add(NightShift);

        foreach (var line in new[] { Line1, Line2 })
        {
            Repo.Lines.Add(line);
            foreach (var type in Types)
            {
                var station = new Station(line.Id, $"{type.ToString().ToUpperInvariant()}-{(line == Line1 ? 1 : 2)}", type);
                _stations[(line.Id, type)] = station;
                Repo.Stations.Add(station);
            }
        }
    }

    public Station Station(ProductionLine line, StationType type) => _stations[(line.Id, type)];

    public void AddHourly(DateOnly date, int hour, ProductionLine line, int planned, int good, Shift? shift = null) =>
        Repo.HourlyRecords.Add(new HourlyRecord(date, hour, line.Id, (shift ?? DayShift).Id, planned, good));

    public void AddTest(DateOnly date, int hour, ProductionLine line, StationType type, int tested, int firstPass) =>
        Repo.TestRecords.Add(new TestRecord(date, hour, Station(line, type).Id, tested, firstPass));

    public void AddDefect(DateOnly date, int hour, ProductionLine line, StationType type, string code, int count) =>
        Repo.DefectEntries.Add(new DefectEntry(date, hour, Station(line, type).Id, code, count));

    /// <summary>Adds the same first-pass rate to every station type of a line for one hour.</summary>
    public void AddTestsForAllTypes(DateOnly date, int hour, ProductionLine line, int tested, int firstPass)
    {
        foreach (var type in Types)
        {
            AddTest(date, hour, line, type, tested, firstPass);
        }
    }

    public static UserContext Operator(ProductionLine line) => new(UserRole.Operator, [line.Id]);

    public static UserContext Supervisor(params ProductionLine[] lines) =>
        new(UserRole.Supervisor, lines.Select(l => l.Id).ToArray());

    /// <summary>A clock fixed at the given plant-local date and time (the plant zone is UTC in tests).</summary>
    public static PlantClock ClockAt(DateOnly date, int hour, int minute = 30) =>
        new(new FixedTimeProvider(new DateTimeOffset(date.ToDateTime(new TimeOnly(hour, minute)), TimeSpan.Zero)), TimeZoneInfo.Utc);

    /// <summary>The usual test moment: today at 10:30.</summary>
    public static PlantClock Now => ClockAt(Today, 10);
}

public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
