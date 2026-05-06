using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class ReturnConfiguration : IEntityTypeConfiguration<Return>
{
    public void Configure(EntityTypeBuilder<Return> builder)
    {
        builder.ToTable("Returns");
        builder.HasKey(returnDoc => returnDoc.Id);
        builder.Property(returnDoc => returnDoc.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(returnDoc => returnDoc.CreatedAt).IsRequired();
        builder.HasOne(returnDoc => returnDoc.FromWarehouse).WithMany().HasForeignKey(returnDoc => returnDoc.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(returnDoc => returnDoc.ToWarehouse).WithMany().HasForeignKey(returnDoc => returnDoc.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(returnDoc => returnDoc.CreatedBy).WithMany().HasForeignKey(returnDoc => returnDoc.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
