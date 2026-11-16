using LossLens.Core.Models;
using LossLens.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Infrastructure.Mock;

/// <summary>
/// Fills the database with demo data: the catalog once, then the clock hours that are missing up to the current
/// one. Running it again later adds only the new hours. Today has data only up to the current hour.
/// </summary>
public sealed class MockDataSeeder(LossLensDbContext context)
{
    public const int HistoryDays = 30;

    private const int ChunkSize = 5_000;

    /// <param name="today">Today's date in the plant time zone.</param>
    /// <param name="currentHour">The current clock hour (0-23) in the plant time zone.</param>
    /// <returns>The number of hourly records added.</returns>
    public async Task<int> SeedAsync(DateOnly today, int currentHour, CancellationToken ct = default)
    {
        var generator = new MockDataGenerator();

        if (!await context.ProductionLines.AnyAsync(ct))
        {
            await AddCatalogAsync(generator.CreateCatalog(), ct);
        }

        var catalog = new MockCatalog(
            await context.ProductionLines.AsNoTracking().ToListAsync(ct),
            await context.Stations.AsNoTracking().ToListAsync(ct),
            await context.Shifts.AsNoTracking().ToListAsync(ct),
            await context.DefectCodes.AsNoTracking().ToListAsync(ct));

        var slots = MissingSlots(await LastSlotAsync(ct), today, currentHour).ToList();
        if (slots.Count == 0)
        {
            return 0;
        }

        var records = generator.CreateRecords(catalog, slots);
        await SaveInChunksAsync(records.Hourly, ct);
        await SaveInChunksAsync(records.Tests, ct);
        await SaveInChunksAsync(records.Defects, ct);

        return records.Hourly.Count;
    }

    /// <summary>The clock hours after the last stored one, up to now. Without data it starts <see cref="HistoryDays"/> ago.</summary>
    private static IEnumerable<(DateOnly Date, int Hour)> MissingSlots((DateOnly Date, int Hour)? last, DateOnly today, int currentHour)
    {
        var start = last is { } l
            ? l.Hour == 23 ? (l.Date.AddDays(1), 0) : (l.Date, l.Hour + 1)
            : (today.AddDays(-(HistoryDays - 1)), 0);

        for (var date = start.Item1; date <= today; date = date.AddDays(1))
        {
            var first = date == start.Item1 ? start.Item2 : 0;
            var last2 = date == today ? currentHour : 23;

            for (var hour = first; hour <= last2; hour++)
            {
                yield return (date, hour);
            }
        }
    }

    private async Task<(DateOnly Date, int Hour)?> LastSlotAsync(CancellationToken ct)
    {
        if (!await context.HourlyRecords.AnyAsync(ct))
        {
            return null;
        }

        var date = await context.HourlyRecords.MaxAsync(r => r.Date, ct);
        var hour = await context.HourlyRecords.Where(r => r.Date == date).MaxAsync(r => r.Hour, ct);

        return (date, hour);
    }

    private async Task AddCatalogAsync(MockCatalog catalog, CancellationToken ct)
    {
        context.DefectCodes.AddRange(catalog.DefectCodes);
        context.Shifts.AddRange(catalog.Shifts);
        context.ProductionLines.AddRange(catalog.Lines);
        await context.SaveChangesAsync(ct);

        context.Stations.AddRange(catalog.Stations);
        await context.SaveChangesAsync(ct);
        context.ChangeTracker.Clear();
    }

    private async Task SaveInChunksAsync<T>(List<T> items, CancellationToken ct) where T : class
    {
        foreach (var chunk in items.Chunk(ChunkSize))
        {
            context.AddRange(chunk);
            await context.SaveChangesAsync(ct);
            context.ChangeTracker.Clear();
        }
    }
}
