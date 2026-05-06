using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class OrderedMaterialConfiguration : IEntityTypeConfiguration<OrderedMaterial>
{
    public void Configure(EntityTypeBuilder<OrderedMaterial> builder)
    {
        builder.ToTable("OrderedMaterials");
        builder.HasKey(orderedMaterial => orderedMaterial.Id);
        builder.Property(orderedMaterial => orderedMaterial.PlannedQuantity).HasPrecision(12, 2).IsRequired();
        builder.Property(orderedMaterial => orderedMaterial.Unit).HasMaxLength(30).IsRequired();
        builder.Property(orderedMaterial => orderedMaterial.CreatedAt).IsRequired();

        builder.HasOne(orderedMaterial => orderedMaterial.WorkOrder)
            .WithMany(workOrder => workOrder.OrderedMaterials)
            .HasForeignKey(orderedMaterial => orderedMaterial.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(orderedMaterial => orderedMaterial.Material)
            .WithMany()
            .HasForeignKey(orderedMaterial => orderedMaterial.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
