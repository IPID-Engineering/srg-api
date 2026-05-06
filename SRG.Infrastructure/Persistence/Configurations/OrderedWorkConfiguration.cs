using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class OrderedWorkConfiguration : IEntityTypeConfiguration<OrderedWork>
{
    public void Configure(EntityTypeBuilder<OrderedWork> builder)
    {
        builder.ToTable("OrderedWorks");
        builder.HasKey(orderedWork => orderedWork.Id);
        builder.Property(orderedWork => orderedWork.Description).HasMaxLength(2000);
        builder.Property(orderedWork => orderedWork.PlannedQuantity).HasPrecision(12, 2).IsRequired();
        builder.Property(orderedWork => orderedWork.Unit).HasMaxLength(30).IsRequired();
        builder.Property(orderedWork => orderedWork.CreatedAt).IsRequired();

        builder.HasOne(orderedWork => orderedWork.WorkOrder)
            .WithMany(workOrder => workOrder.OrderedWorks)
            .HasForeignKey(orderedWork => orderedWork.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(orderedWork => orderedWork.WorkType)
            .WithMany(workType => workType.OrderedWorks)
            .HasForeignKey(orderedWork => orderedWork.WorkTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(orderedWork => orderedWork.Section)
            .WithMany()
            .HasForeignKey(orderedWork => orderedWork.SectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(orderedWork => orderedWork.Installation)
            .WithMany()
            .HasForeignKey(orderedWork => orderedWork.InstallationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
