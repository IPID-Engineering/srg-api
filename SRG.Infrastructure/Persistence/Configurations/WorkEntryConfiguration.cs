using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WorkEntryConfiguration : IEntityTypeConfiguration<WorkEntry>
{
    public void Configure(EntityTypeBuilder<WorkEntry> builder)
    {
        builder.ToTable("WorkEntries");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Description)
            .HasMaxLength(2000);

        builder.Property(entry => entry.Quantity)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(entry => entry.WorkerCount)
            .HasDefaultValue(0);

        builder.Property(entry => entry.HoursSpent)
            .HasPrecision(8, 2)
            .HasDefaultValue(0);

        builder.Property(entry => entry.IsAddedByForeman)
            .HasDefaultValue(false);

        builder.HasOne(entry => entry.WorkType)
            .WithMany(workType => workType.WorkEntries)
            .HasForeignKey(entry => entry.WorkTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.OrderedWork)
            .WithMany(orderedWork => orderedWork.WorkEntries)
            .HasForeignKey(entry => entry.OrderedWorkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.DailyReport)
            .WithMany(dailyReport => dailyReport.WorkEntries)
            .HasForeignKey(entry => entry.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
