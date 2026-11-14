using LossLens.Core.Metrics;

namespace LossLens.Core.Tests;

public class PlanAttainmentCalculatorTests
{
    [Theory]
    [InlineData(100, 92, 92.0)]
    [InlineData(300, 150, 50.0)]
    [InlineData(100, 110, 110.0)]
    [InlineData(100, 0, 0.0)]
    public void Calculate_ReturnsPercentage(int planned, int good, double expected)
    {
        var result = PlanAttainmentCalculator.Calculate(planned, good);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value, precision: 1);
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(0, 0)]
    public void Calculate_ReturnsNull_WhenNothingPlanned(int planned, int good)
    {
        Assert.Null(PlanAttainmentCalculator.Calculate(planned, good));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(100, -1)]
    public void Calculate_Throws_OnNegativeInput(int planned, int good)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PlanAttainmentCalculator.Calculate(planned, good));
    }
}
