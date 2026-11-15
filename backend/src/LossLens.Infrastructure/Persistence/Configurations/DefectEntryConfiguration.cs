using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LossLens.Infrastructure.Persistence.Configurations;

public class DefectEntryConfiguration : IEntityTypeConfiguration<DefectEntry>
{
    public void Configure(EntityTypeBuilder<DefectEntry> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.DefectCode).IsRequired().HasMaxLength(20);

        builder.HasOne<Station>().WithMany()
            .HasForeignKey(e => e.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        // The foreign key points to the defect code itself, not to a Guid.
        builder.HasOne<DefectCode>().WithMany()
            .HasForeignKey(e => e.DefectCode)
            .HasPrincipalKey(c => c.Code)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per defect code, station and hour.
        builder.HasIndex(e => new { e.Date, e.StationId, e.Hour, e.DefectCode }).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_DefectEntries_Hour", "\"Hour\" BETWEEN 0 AND 23");
            t.HasCheckConstraint("CK_DefectEntries_Count", "\"Count\" >= 0");
        });
    }
}
