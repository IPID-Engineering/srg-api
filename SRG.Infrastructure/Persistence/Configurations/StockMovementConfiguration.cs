using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        builder.HasKey(movement => movement.Id);
        builder.Property(movement => movement.Quantity).HasPrecision(12, 2).IsRequired();
        builder.Property(movement => movement.Direction).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(movement => movement.SourceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(movement => movement.CreatedAt).IsRequired();
        builder.HasIndex(movement => new { movement.WarehouseId, movement.CreatedAt });
        builder.HasIndex(movement => new { movement.SourceType, movement.SourceId });

        builder.HasOne(movement => movement.Warehouse)
            .WithMany()
            .HasForeignKey(movement => movement.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.Material)
            .WithMany()
            .HasForeignKey(movement => movement.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(movement => movement.CreatedBy)
            .WithMany()
            .HasForeignKey(movement => movement.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
