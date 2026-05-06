using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class ReturnItemConfiguration : IEntityTypeConfiguration<ReturnItem>
{
    public void Configure(EntityTypeBuilder<ReturnItem> builder)
    {
        builder.ToTable("ReturnItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Quantity).HasPrecision(12, 2).IsRequired();
        builder.HasOne(item => item.Return).WithMany(returnDoc => returnDoc.Items).HasForeignKey(item => item.ReturnId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
    }
}
