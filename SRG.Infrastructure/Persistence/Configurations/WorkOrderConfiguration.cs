using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");
        builder.HasKey(workOrder => workOrder.Id);
        builder.Property(workOrder => workOrder.Number).HasMaxLength(80).IsRequired();
        builder.Property(workOrder => workOrder.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(workOrder => workOrder.Description).HasMaxLength(4000);
        builder.Property(workOrder => workOrder.CreatedAt).IsRequired();
        builder.HasIndex(workOrder => workOrder.Number).IsUnique();

        builder.HasOne(workOrder => workOrder.Project)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.Section)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.Crew)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.CrewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.Subcontractor)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(workOrder => workOrder.CreatedBy)
            .WithMany()
            .HasForeignKey(workOrder => workOrder.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
