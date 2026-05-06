using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class GoodsReceivedVoucherItemConfiguration : IEntityTypeConfiguration<GoodsReceivedVoucherItem>
{
    public void Configure(EntityTypeBuilder<GoodsReceivedVoucherItem> builder)
    {
        builder.ToTable("GoodsReceivedVoucherItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.PartNumber).HasMaxLength(100);
        builder.Property(item => item.VendorPartNumber).HasMaxLength(100);
        builder.Property(item => item.Quantity).HasPrecision(12, 2).IsRequired();
        builder.Property(item => item.Unit).HasMaxLength(30).IsRequired();
        builder.Property(item => item.UnitPrice).HasPrecision(12, 2);
        builder.Property(item => item.ExtendedPrice).HasPrecision(14, 2);
        builder.Property(item => item.Status).HasMaxLength(50);

        builder.HasOne(item => item.GoodsReceivedVoucher)
            .WithMany(grv => grv.Items)
            .HasForeignKey(item => item.GoodsReceivedVoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Material)
            .WithMany()
            .HasForeignKey(item => item.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
