using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.ToTable("Issues");
        builder.HasKey(issue => issue.Id);
        builder.Property(issue => issue.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(issue => issue.CreatedAt).IsRequired();
        builder.HasOne(issue => issue.WorkOrder).WithMany(wo => wo.Issues).HasForeignKey(issue => issue.WorkOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(issue => issue.FromWarehouse).WithMany().HasForeignKey(issue => issue.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(issue => issue.ToWarehouse).WithMany().HasForeignKey(issue => issue.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(issue => issue.CreatedBy).WithMany().HasForeignKey(issue => issue.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}
