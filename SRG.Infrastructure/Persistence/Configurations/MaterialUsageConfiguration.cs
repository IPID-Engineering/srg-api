using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class MaterialUsageConfiguration : IEntityTypeConfiguration<MaterialUsage>
{
    public void Configure(EntityTypeBuilder<MaterialUsage> builder)
    {
        builder.ToTable("MaterialUsages");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Quantity)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.HasOne(entry => entry.Material)
            .WithMany()
            .HasForeignKey(entry => entry.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.OrderedMaterial)
            .WithMany(orderedMaterial => orderedMaterial.MaterialUsages)
            .HasForeignKey(entry => entry.OrderedMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.DailyReport)
            .WithMany(dailyReport => dailyReport.MaterialUsages)
            .HasForeignKey(entry => entry.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
