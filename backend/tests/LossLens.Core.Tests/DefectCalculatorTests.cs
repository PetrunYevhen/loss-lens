using LossLens.Core.Metrics;

namespace LossLens.Core.Tests;

public class DefectCalculatorTests
{
    [Theory]
    [InlineData(12, 500, 200, 120.0)]
    [InlineData(0, 500, 200, 0.0)]
    [InlineData(1, 1_000_000, 1, 1.0)]
    public void Dpmo_ReturnsDefectsPerMillionOpportunities(
        int defects, int unitsInspected, int opportunities, double expected)
    {
        var result = DefectCalculator.Dpmo(defects, unitsInspected, opportunities);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value, precision: 1);
    }

    [Theory]
    [InlineData(5, 0, 200)]
    [InlineData(5, 10, 0)]
    [InlineData(5, 10, -1)]
    public void Dpmo_ReturnsNull_WhenThereAreNoOpportunities(int defects, int unitsInspected, int opportunities)
    {
        Assert.Null(DefectCalculator.Dpmo(defects, unitsInspected, opportunities));
    }

    [Fact]
    public void Dpmo_DoesNotOverflow_OnLargeVolumes()
    {
        var result = DefectCalculator.Dpmo(1, int.MaxValue, 200);

        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Theory]
    [InlineData(-1, 10, 200)]
    [InlineData(1, -10, 200)]
    public void Dpmo_Throws_OnNegativeInput(int defects, int unitsInspected, int opportunities)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DefectCalculator.Dpmo(defects, unitsInspected, opportunities));
    }

    [Fact]
    public void Pareto_SortsByCount_AndCalculatesShares()
    {
        var result = DefectCalculator.Pareto(new[] { ("B", 30), ("A", 50), ("D", 5), ("C", 15) });

        Assert.Equal(new[] { "A", "B", "C", "D" }, result.Select(i => i.Code));
        Assert.Equal(new[] { 50, 30, 15, 5 }, result.Select(i => i.Count));
        Assert.Equal(new[] { 50.0, 30.0, 15.0, 5.0 }, result.Select(i => i.Share));
        Assert.Equal(new[] { 50.0, 80.0, 95.0, 100.0 }, result.Select(i => i.CumulativeShare));
    }

    [Fact]
    public void Pareto_OrdersEqualCountsByCode()
    {
        var result = DefectCalculator.Pareto(new[] { ("B", 1), ("A", 1), ("C", 1) });

        Assert.Equal(new[] { "A", "B", "C" }, result.Select(i => i.Code));
    }

    [Fact]
    public void Pareto_LastCumulativeShareIsExactlyOneHundred()
    {
        var result = DefectCalculator.Pareto(new[] { ("A", 1), ("B", 1), ("C", 1) });

        Assert.Equal(100.0, result[^1].CumulativeShare);
    }

    [Fact]
    public void Pareto_SumsDuplicateCodes()
    {
        var result = DefectCalculator.Pareto(new[] { ("A", 2), ("B", 1), ("A", 3) });

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Code);
        Assert.Equal(5, result[0].Count);
    }

    [Fact]
    public void Pareto_ExcludesCodesWithZeroCount()
    {
        var result = DefectCalculator.Pareto(new[] { ("A", 4), ("B", 0) });

        var item = Assert.Single(result);
        Assert.Equal("A", item.Code);
        Assert.Equal(100.0, item.Share);
    }

    [Fact]
    public void Pareto_ReturnsEmpty_WhenNoDefects()
    {
        Assert.Empty(DefectCalculator.Pareto(Array.Empty<(string, int)>()));
        Assert.Empty(DefectCalculator.Pareto(new[] { ("A", 0), ("B", 0) }));
    }

    [Fact]
    public void Pareto_Throws_OnNegativeCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DefectCalculator.Pareto(new[] { ("A", -1) }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Pareto_Throws_OnBlankCode(string code)
    {
        Assert.Throws<ArgumentException>(
            () => DefectCalculator.Pareto(new[] { (code, 1) }));
    }

    [Fact]
    public void Pareto_Throws_WhenItemsIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => DefectCalculator.Pareto(null!));
    }
}
