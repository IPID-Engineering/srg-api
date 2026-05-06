using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class GoodsReceivedVoucherConfiguration : IEntityTypeConfiguration<GoodsReceivedVoucher>
{
    public void Configure(EntityTypeBuilder<GoodsReceivedVoucher> builder)
    {
        builder.ToTable("GoodsReceivedVouchers");
        builder.HasKey(grv => grv.Id);
        builder.Property(grv => grv.Number).HasMaxLength(80).IsRequired();
        builder.Property(grv => grv.SupplierName).HasMaxLength(200);
        builder.Property(grv => grv.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(grv => grv.DeliveryDate).IsRequired();
        builder.Property(grv => grv.CreatedAt).IsRequired();
        builder.HasIndex(grv => grv.Number).IsUnique();

        builder.HasOne(grv => grv.Warehouse)
            .WithMany()
            .HasForeignKey(grv => grv.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(grv => grv.CreatedBy)
            .WithMany()
            .HasForeignKey(grv => grv.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
