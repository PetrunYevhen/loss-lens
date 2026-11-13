using LossLens.Core.Metrics;

namespace LossLens.Core.Tests;

public class FpyCalculatorTests
{
    [Theory]
    [InlineData(100, 97, 97.0)]
    [InlineData(100, 100, 100.0)]
    [InlineData(100, 0, 0.0)]
    [InlineData(3, 2, 66.7)]
    public void StationFpy_ReturnsPercentage(int tested, int firstPass, double expected)
    {
        var result = FpyCalculator.StationFpy(tested, firstPass);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value, precision: 1);
    }

    [Fact]
    public void StationFpy_ReturnsNull_WhenNothingTested()
    {
        Assert.Null(FpyCalculator.StationFpy(0, 0));
    }

    [Theory]
    [InlineData(10, 11)]
    [InlineData(-1, 0)]
    [InlineData(10, -1)]
    public void StationFpy_Throws_OnInvalidInput(int tested, int firstPass)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FpyCalculator.StationFpy(tested, firstPass));
    }

    [Fact]
    public void LineRty_MultipliesStationFpys()
    {
        var result = FpyCalculator.LineRty(new double?[] { 98, 96, 97, 99 });

        Assert.NotNull(result.Value);
        Assert.Equal(90.3, result.Value.Value, precision: 1);
        Assert.False(result.Incomplete);
    }

    [Fact]
    public void LineRty_SkipsStationWithoutData_AndMarksIncomplete()
    {
        var result = FpyCalculator.LineRty(new double?[] { 98, null, 97, 99 });

        Assert.NotNull(result.Value);
        Assert.Equal(94.1, result.Value.Value, precision: 1);
        Assert.True(result.Incomplete);
    }

    [Fact]
    public void LineRty_ReturnsStationValue_ForSingleStation()
    {
        var result = FpyCalculator.LineRty(new double?[] { 97 });

        Assert.NotNull(result.Value);
        Assert.Equal(97.0, result.Value.Value, precision: 1);
        Assert.False(result.Incomplete);
    }

    [Fact]
    public void LineRty_ReturnsNullAndIncomplete_WhenAllStationsMissing()
    {
        var result = FpyCalculator.LineRty(new double?[] { null, null });

        Assert.Null(result.Value);
        Assert.True(result.Incomplete);
    }

    [Fact]
    public void LineRty_ReturnsNullAndIncomplete_WhenNoStations()
    {
        var result = FpyCalculator.LineRty(Array.Empty<double?>());

        Assert.Null(result.Value);
        Assert.True(result.Incomplete);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void LineRty_Throws_WhenFpyOutOfRange(double fpy)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FpyCalculator.LineRty(new double?[] { fpy }));
    }

    [Fact]
    public void LineRty_Throws_WhenCollectionIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => FpyCalculator.LineRty(null!));
    }
}
