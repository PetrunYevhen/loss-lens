using LossLens.Core.Abstractions;
using LossLens.Core.Models;
using LossLens.Core.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Infrastructure.Persistence;

/// <summary>
/// Creates the demo users: <c>manager</c>, <c>supervisor</c> (all lines), and an operator for each of the first
/// two lines (<c>operator1</c>, <c>operator2</c>). Users that already exist are left alone, and users that need a
/// line are created only once lines exist, so running it again later fills the gaps.
/// </summary>
public sealed class DemoUserSeeder(LossLensDbContext context, IPasswordHasher passwordHasher)
{
    /// <returns>The number of users created.</returns>
    public async Task<int> SeedAsync(string password, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var lines = await context.ProductionLines.AsNoTracking().OrderBy(l => l.Name).ToListAsync(ct);
        var existing = (await context.Users.Select(u => u.Username).ToListAsync(ct)).ToHashSet();
        var hash = passwordHasher.Hash(password);

        var wanted = new List<User> { new("manager", hash, UserRole.Manager, []) };
        if (lines.Count > 0)
        {
            wanted.Add(new User("supervisor", hash, UserRole.Supervisor, lines.Select(l => l.Id)));
        }

        for (var i = 0; i < Math.Min(2, lines.Count); i++)
        {
            wanted.Add(new User($"operator{i + 1}", hash, UserRole.Operator, [lines[i].Id]));
        }

        var created = wanted.Where(u => !existing.Contains(u.Username)).ToList();
        context.Users.AddRange(created);
        await context.SaveChangesAsync(ct);

        return created.Count;
    }
}
