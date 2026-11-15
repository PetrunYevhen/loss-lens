using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LossLens.Infrastructure.Persistence.Configurations;

public class TestRecordConfiguration : IEntityTypeConfiguration<TestRecord>
{
    public void Configure(EntityTypeBuilder<TestRecord> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.HasOne<Station>().WithMany()
            .HasForeignKey(r => r.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One record per station and hour.
        builder.HasIndex(r => new { r.Date, r.StationId, r.Hour }).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_TestRecords_Hour", "\"Hour\" BETWEEN 0 AND 23");
            t.HasCheckConstraint(
                "CK_TestRecords_Units",
                "\"TestedUnits\" >= 0 AND \"FirstPassUnits\" >= 0 AND \"FirstPassUnits\" <= \"TestedUnits\"");
        });
    }
}
