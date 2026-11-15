using LossLens.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LossLens.Infrastructure.Persistence.Configurations;

public class DefectCodeConfiguration : IEntityTypeConfiguration<DefectCode>
{
    public void Configure(EntityTypeBuilder<DefectCode> builder)
    {
        builder.HasKey(c => c.Code);
        builder.Property(c => c.Code).HasMaxLength(20).ValueGeneratedNever();
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Category).IsRequired().HasMaxLength(50);
    }
}
