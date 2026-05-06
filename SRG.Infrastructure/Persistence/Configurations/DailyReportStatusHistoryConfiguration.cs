using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class DailyReportStatusHistoryConfiguration : IEntityTypeConfiguration<DailyReportStatusHistory>
{
    public void Configure(EntityTypeBuilder<DailyReportStatusHistory> builder)
    {
        builder.ToTable("daily_report_status_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.DailyReportId).HasColumnName("daily_report_id").IsRequired();
        builder.Property(h => h.FromStatus).HasColumnName("from_status").IsRequired();
        builder.Property(h => h.ToStatus).HasColumnName("to_status").IsRequired();
        builder.Property(h => h.Reason).HasColumnName("reason");
        builder.Property(h => h.ChangedById).HasColumnName("changed_by_id").IsRequired();
        builder.Property(h => h.ChangedAt).HasColumnName("changed_at").IsRequired();

        builder.HasOne(h => h.DailyReport)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(h => h.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
