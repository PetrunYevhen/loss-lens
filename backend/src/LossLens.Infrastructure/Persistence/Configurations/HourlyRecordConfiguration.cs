using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LossLens.Infrastructure.Persistence.Configurations;

public class HourlyRecordConfiguration : IEntityTypeConfiguration<HourlyRecord>
{
    public void Configure(EntityTypeBuilder<HourlyRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.HasOne<ProductionLine>().WithMany()
            .HasForeignKey(r => r.LineId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Shift>().WithMany()
            .HasForeignKey(r => r.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Queries filter by date range, then optionally by line. One record per line and hour.
        builder.HasIndex(r => new { r.Date, r.LineId, r.Hour }).IsUnique();

        // The same invariants as in the constructor, enforced by the database too.
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_HourlyRecords_Hour", "\"Hour\" BETWEEN 0 AND 23");
            t.HasCheckConstraint("CK_HourlyRecords_Units", "\"PlannedUnits\" >= 0 AND \"GoodUnits\" >= 0");
        });
    }
}
