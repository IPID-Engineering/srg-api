using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.ToTable("DailyReports");

        builder.HasKey(dailyReport => dailyReport.Id);

        builder.HasIndex(dailyReport => new { dailyReport.Date, dailyReport.CrewId })
            .IsUnique()
            .HasFilter("\"CrewId\" IS NOT NULL");

        builder.HasIndex(dailyReport => new { dailyReport.Date, dailyReport.SubcontractorCrewId })
            .IsUnique()
            .HasFilter("\"SubcontractorCrewId\" IS NOT NULL");

        builder.Property(dailyReport => dailyReport.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(dailyReport => dailyReport.Notes)
            .HasMaxLength(4000);

        builder.Property(dailyReport => dailyReport.RejectionReason)
            .HasMaxLength(2000);

        builder.Property(dailyReport => dailyReport.CreatedAt)
            .IsRequired();

        builder.HasOne(dailyReport => dailyReport.Crew)
            .WithMany(crew => crew.DailyReports)
            .HasForeignKey(dailyReport => dailyReport.CrewId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dailyReport => dailyReport.SubcontractorCrew)
            .WithMany(crew => crew.DailyReports)
            .HasForeignKey(dailyReport => dailyReport.SubcontractorCrewId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dailyReport => dailyReport.Project)
            .WithMany(project => project.DailyReports)
            .HasForeignKey(dailyReport => dailyReport.ProjectId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dailyReport => dailyReport.Section)
            .WithMany(section => section.DailyReports)
            .HasForeignKey(dailyReport => dailyReport.SectionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dailyReport => dailyReport.WorkOrder)
            .WithMany(workOrder => workOrder.DailyReports)
            .HasForeignKey(dailyReport => dailyReport.WorkOrderId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dailyReport => dailyReport.CreatedBy)
            .WithMany()
            .HasForeignKey(dailyReport => dailyReport.CreatedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
