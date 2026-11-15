using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LossLens.Infrastructure.Persistence;

public class LossLensDbContext(DbContextOptions<LossLensDbContext> options) : DbContext(options)
{
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<HourlyRecord> HourlyRecords => Set<HourlyRecord>();
    public DbSet<TestRecord> TestRecords => Set<TestRecord>();
    public DbSet<DefectCode> DefectCodes => Set<DefectCode>();
    public DbSet<DefectEntry> DefectEntries => Set<DefectEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LossLensDbContext).Assembly);
}
