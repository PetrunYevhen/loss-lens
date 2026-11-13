namespace LossLens.Core.Metrics;

public static class DefectCalculator
{
    /// <summary>
    /// Defects Per Million Opportunities, or null when it cannot be calculated
    /// (no inspected units or no opportunities per unit).
    /// </summary>
    public static double? Dpmo(int defects, int unitsInspected, int opportunitiesPerUnit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(defects);
        ArgumentOutOfRangeException.ThrowIfNegative(unitsInspected);

        if (unitsInspected == 0 || opportunitiesPerUnit <= 0)
            return null;

        // Multiply as double: int * int can overflow on large volumes.
        var opportunities = (double)unitsInspected * opportunitiesPerUnit;

        return defects / opportunities * 1_000_000;
    }

    /// <summary>
    /// Defect causes sorted by count (descending, then by code) with share and cumulative share in percent.
    /// Duplicate codes are summed. Returns an empty list when there are no defects.
    /// </summary>
    public static IReadOnlyList<ParetoItem> Pareto(IEnumerable<(string Code, int Count)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var grouped = items
            .Select(item =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(item.Code);
                ArgumentOutOfRangeException.ThrowIfNegative(item.Count);
                return item;
            })
            .GroupBy(item => item.Code)
            .Select(group => (Code: group.Key, Count: group.Sum(item => item.Count)))
            .Where(item => item.Count > 0)
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ToList();

        var total = grouped.Sum(item => item.Count);
        if (total == 0)
            return [];

        var result = new List<ParetoItem>(grouped.Count);
        var running = 0;

        foreach (var (code, count) in grouped)
        {
            running += count;

            // Divide the integer running sum (not a sum of doubles) so the last item is exactly 100.
            result.Add(new ParetoItem(
                code,
                count,
                Share: count / (double)total * 100,
                CumulativeShare: running / (double)total * 100));
        }

        return result;
    }
}
