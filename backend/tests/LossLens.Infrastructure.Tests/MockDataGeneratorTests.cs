using LossLens.Core.Models.Enums;
using LossLens.Infrastructure.Mock;

namespace LossLens.Infrastructure.Tests;

public class MockDataGeneratorTests
{
    private static readonly DateOnly Day = new(2026, 11, 12);

    private readonly MockDataGenerator _generator = new();

    private static IEnumerable<(DateOnly, int)> WholeDay(DateOnly date) => Enumerable.Range(0, 24).Select(h => (date, h));

    [Fact]
    public void Catalog_HasThreeLinesFourStationsEachThreeShiftsAndDefectCodes()
    {
        var catalog = _generator.CreateCatalog();

        Assert.Equal(new[] { "Line 1", "Line 2", "Line 3" }, catalog.Lines.Select(l => l.Name));
        Assert.Equal(12, catalog.Stations.Count);
        Assert.All(catalog.Lines, line =>
            Assert.Equal(
                new[] { StationType.Spi, StationType.Aoi, StationType.Ict, StationType.Fct },
                catalog.Stations.Where(s => s.LineId == line.Id).Select(s => s.Type).Order()));
        Assert.Equal(new[] { "A", "B", "C" }, catalog.Shifts.Select(s => s.Name));
        Assert.Equal(catalog.DefectCodes.Count, catalog.DefectCodes.Select(c => c.Code).Distinct().Count());
    }

    [Fact]
    public void EveryHourOfTheDay_BelongsToExactlyOneShift()
    {
        var shifts = _generator.CreateCatalog().Shifts;

        for (var hour = 0; hour < 24; hour++)
        {
            Assert.Single(shifts, s => s.Contains(hour));
        }
    }

    [Fact]
    public void SameInput_GivesIdenticalNumbers()
    {
        var catalog = _generator.CreateCatalog();

        var first = new MockDataGenerator().CreateRecords(catalog, WholeDay(Day));
        var second = new MockDataGenerator().CreateRecords(catalog, WholeDay(Day));

        Assert.Equal(first.Hourly.Select(r => (r.Date, r.Hour, r.LineId, r.PlannedUnits, r.GoodUnits)),
                     second.Hourly.Select(r => (r.Date, r.Hour, r.LineId, r.PlannedUnits, r.GoodUnits)));
        Assert.Equal(first.Tests.Select(r => (r.StationId, r.Hour, r.TestedUnits, r.FirstPassUnits)),
                     second.Tests.Select(r => (r.StationId, r.Hour, r.TestedUnits, r.FirstPassUnits)));
        Assert.Equal(first.Defects.Select(r => (r.StationId, r.Hour, r.DefectCode, r.Count)),
                     second.Defects.Select(r => (r.StationId, r.Hour, r.DefectCode, r.Count)));
    }

    [Fact]
    public void AnHour_GetsTheSameNumbersWhetherGeneratedAloneOrWithTheWholeDay()
    {
        var catalog = _generator.CreateCatalog();

        var whole = _generator.CreateRecords(catalog, WholeDay(Day));
        var alone = _generator.CreateRecords(catalog, [(Day, 9)]);

        Assert.Equal(
            whole.Hourly.Where(r => r.Hour == 9).Select(r => (r.LineId, r.PlannedUnits, r.GoodUnits)),
            alone.Hourly.Select(r => (r.LineId, r.PlannedUnits, r.GoodUnits)));
    }

    [Fact]
    public void ADifferentSeed_GivesDifferentNumbers()
    {
        var catalog = _generator.CreateCatalog();

        var a = new MockDataGenerator(seed: 1).CreateRecords(catalog, WholeDay(Day));
        var b = new MockDataGenerator(seed: 2).CreateRecords(catalog, WholeDay(Day));

        Assert.NotEqual(a.Hourly.Select(r => r.PlannedUnits), b.Hourly.Select(r => r.PlannedUnits));
    }

    [Fact]
    public void OneRecordPerLineAndHour_PlanWithinRange_AndOnlyTheRequestedHours()
    {
        var catalog = _generator.CreateCatalog();

        var records = _generator.CreateRecords(catalog, [(Day, 8), (Day, 9)]);

        Assert.Equal(6, records.Hourly.Count);
        Assert.Equal(6, records.Hourly.Select(r => (r.LineId, r.Hour)).Distinct().Count());
        Assert.All(records.Hourly, r => Assert.InRange(r.PlannedUnits, 80, 120));
        Assert.All(records.Hourly, r => Assert.Contains(r.Hour, new[] { 8, 9 }));
        Assert.Equal(24, records.Tests.Count);
    }

    [Fact]
    public void TestRecords_AreConsistent_AndDefectsAddUpToTheFailures()
    {
        var catalog = _generator.CreateCatalog();

        var records = _generator.CreateRecords(catalog, WholeDay(Day));

        Assert.All(records.Tests, t => Assert.InRange(t.FirstPassUnits, 0, t.TestedUnits));

        var defectsByStationHour = records.Defects
            .GroupBy(d => (d.StationId, d.Hour))
            .ToDictionary(g => g.Key, g => g.Sum(d => d.Count));
        foreach (var test in records.Tests)
        {
            defectsByStationHour.TryGetValue((test.StationId, test.Hour), out var defects);
            Assert.Equal(test.FailedUnits, defects);
        }
    }

    [Fact]
    public void Defects_UseCatalogCodes_OneRowPerCodeStationAndHour()
    {
        var catalog = _generator.CreateCatalog();
        var codes = catalog.DefectCodes.Select(c => c.Code).ToHashSet();

        var records = _generator.CreateRecords(catalog, WholeDay(Day));

        Assert.All(records.Defects, d => Assert.Contains(d.DefectCode, codes));
        Assert.All(records.Defects, d => Assert.True(d.Count > 0));
        Assert.Equal(
            records.Defects.Count,
            records.Defects.Select(d => (d.StationId, d.Hour, d.DefectCode)).Distinct().Count());
    }

    [Fact]
    public void NormalStationYield_StaysInTheRealisticRange()
    {
        var catalog = _generator.CreateCatalog();
        var dayShiftHours = Enumerable.Range(6, 8).Select(h => (Day, h));

        var records = _generator.CreateRecords(catalog, dayShiftHours);

        // Outside the planted anomalies the yield of a station over a shift sits between 94 and 99.5 percent.
        foreach (var group in records.Tests.GroupBy(t => t.StationId))
        {
            var yield = group.Sum(t => t.FirstPassUnits) / (double)group.Sum(t => t.TestedUnits) * 100;
            Assert.InRange(yield, 94, 99.6);
        }
    }

    [Fact]
    public void ShiftC_HasALowerYieldThanShiftA()
    {
        var catalog = _generator.CreateCatalog();
        var shiftA = Enumerable.Range(6, 8).Select(h => (Day, h));
        var shiftC = Enumerable.Range(22, 2).Select(h => (Day, h)).Concat(Enumerable.Range(0, 6).Select(h => (Day, h)));

        double Yield(IEnumerable<(DateOnly, int)> slots)
        {
            var tests = _generator.CreateRecords(catalog, slots).Tests;
            return tests.Sum(t => t.FirstPassUnits) / (double)tests.Sum(t => t.TestedUnits);
        }

        Assert.True(Yield(shiftC) < Yield(shiftA) - 0.02);
    }

    [Fact]
    public void Line3_ProducesAlmostNothingInHourTenOnOneWeekday()
    {
        var catalog = _generator.CreateCatalog();
        var line3 = catalog.Lines.Single(l => l.Name == "Line 3");
        var spi = catalog.Stations.Single(s => s.LineId == line3.Id && s.Type == StationType.Spi);
        var days = Enumerable.Range(0, 7).Select(i => Day.AddDays(i)).ToList();

        var records = _generator.CreateRecords(catalog, days.Select(d => (d, 10)));

        var stalled = records.Tests.Where(t => t.StationId == spi.Id && t.TestedUnits < 10).ToList();
        Assert.Single(stalled);
    }
}
