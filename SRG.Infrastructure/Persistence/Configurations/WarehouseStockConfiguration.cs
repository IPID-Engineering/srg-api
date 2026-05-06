using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WarehouseStockConfiguration : IEntityTypeConfiguration<WarehouseStock>
{
    public void Configure(EntityTypeBuilder<WarehouseStock> builder)
    {
        builder.ToTable("WarehouseStocks");
        builder.HasKey(stock => stock.Id);
        builder.HasIndex(stock => new { stock.WarehouseId, stock.MaterialId }).IsUnique();
        builder.Property(stock => stock.Quantity).HasPrecision(12, 2).IsRequired();
        builder.HasOne(stock => stock.Warehouse)
            .WithMany(warehouse => warehouse.WarehouseStocks)
            .HasForeignKey(stock => stock.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(stock => stock.Material)
            .WithMany(material => material.WarehouseStocks)
            .HasForeignKey(stock => stock.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
