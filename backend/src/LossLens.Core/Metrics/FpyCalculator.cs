namespace LossLens.Core.Metrics;

public static class FpyCalculator
{
    /// <summary>First Pass Yield of one station in percent, or null when nothing was tested.</summary>
    public static double? StationFpy(int tested, int firstPass)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tested);
        ArgumentOutOfRangeException.ThrowIfNegative(firstPass);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstPass, tested);

        if (tested == 0)
            return null;

        return firstPass / (double)tested * 100;
    }

    /// <summary>
    /// Rolled Throughput Yield of a line in percent: the product of station FPYs.
    /// Stations without data (null) are skipped and mark the result as incomplete.
    /// </summary>
    public static RtyResult LineRty(IEnumerable<double?> stationFpys)
    {
        ArgumentNullException.ThrowIfNull(stationFpys);

        var product = 1.0;
        var hasValue = false;
        var incomplete = false;

        foreach (var fpy in stationFpys)
        {
            if (fpy is null)
            {
                incomplete = true;
                continue;
            }

            ArgumentOutOfRangeException.ThrowIfNegative(fpy.Value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(fpy.Value, 100);

            product *= fpy.Value / 100;
            hasValue = true;
        }

        return hasValue
            ? new RtyResult(product * 100, incomplete)
            : new RtyResult(null, true);
    }
}
