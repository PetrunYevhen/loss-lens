using LossLens.Core.Config;
using LossLens.Core.Metrics;
using LossLens.Core.Models.Enums;

namespace LossLens.Core.Tests;

public class RagEvaluatorTests
{
    private static readonly Threshold PlanThreshold = new(95, 85);

    [Theory]
    [InlineData(100.0, RagStatus.Green)]
    [InlineData(96.0, RagStatus.Green)]
    [InlineData(95.0, RagStatus.Green)]
    [InlineData(94.99, RagStatus.Amber)]
    [InlineData(90.0, RagStatus.Amber)]
    [InlineData(85.0, RagStatus.Amber)]
    [InlineData(84.99, RagStatus.Red)]
    [InlineData(0.0, RagStatus.Red)]
    [InlineData(null, RagStatus.Unknown)]
    public void Evaluate_ReturnsStatusForValue(double? value, RagStatus expected)
    {
        Assert.Equal(expected, RagEvaluator.Evaluate(value, PlanThreshold));
    }

    [Fact]
    public void Evaluate_UsesTheGivenThreshold()
    {
        Assert.Equal(RagStatus.Green, RagEvaluator.Evaluate(93, new Threshold(92, 88)));
        Assert.Equal(RagStatus.Red, RagEvaluator.Evaluate(93, new Threshold(97, 94)));
    }

    [Fact]
    public void Evaluate_Throws_WhenThresholdIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => RagEvaluator.Evaluate(90, null!));
    }
}
