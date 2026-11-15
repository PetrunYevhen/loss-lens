using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LossLens.Infrastructure.Persistence.Configurations;

public class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.Name).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Type).HasConversion<string>().HasMaxLength(10);

        builder.HasOne<ProductionLine>().WithMany()
            .HasForeignKey(s => s.LineId)
            .OnDelete(DeleteBehavior.Restrict);

        // One station of each type per line.
        builder.HasIndex(s => new { s.LineId, s.Type }).IsUnique();
    }
}
