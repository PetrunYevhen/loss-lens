namespace LossLens.Core.Metrics;

public static class PlanAttainmentCalculator
{
    public static double? Calculate(int planned, int good)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(planned);
        ArgumentOutOfRangeException.ThrowIfNegative(good);

        if (planned == 0)
            return null;

        return good / (double)planned * 100;
    }
}