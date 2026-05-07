using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class DailyReportWorkOrderConfiguration : IEntityTypeConfiguration<DailyReportWorkOrder>
{
    public void Configure(EntityTypeBuilder<DailyReportWorkOrder> builder)
    {
        builder.ToTable("DailyReportWorkOrders");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.DailyReportId, x.WorkOrderId }).IsUnique();

        builder.HasOne(x => x.DailyReport)
            .WithMany(x => x.DailyReportWorkOrders)
            .HasForeignKey(x => x.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.WorkOrder)
            .WithMany(x => x.DailyReportWorkOrders)
            .HasForeignKey(x => x.WorkOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
