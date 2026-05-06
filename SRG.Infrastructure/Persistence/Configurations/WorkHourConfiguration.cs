using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WorkHourConfiguration : IEntityTypeConfiguration<WorkHour>
{
    public void Configure(EntityTypeBuilder<WorkHour> builder)
    {
        builder.ToTable("WorkHours");

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.Hours)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_WorkHours_ExactlyOneWorker",
            "(\"WorkerId\" IS NOT NULL AND \"SubcontractorWorkerId\" IS NULL) OR (\"WorkerId\" IS NULL AND \"SubcontractorWorkerId\" IS NOT NULL)"));

        builder.HasOne(entry => entry.DailyReport)
            .WithMany(dailyReport => dailyReport.WorkHours)
            .HasForeignKey(entry => entry.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entry => entry.Worker)
            .WithMany(person => person.WorkHour)
            .HasForeignKey(entry => entry.WorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entry => entry.SubcontractorWorker)
            .WithMany(worker => worker.WorkHours)
            .HasForeignKey(entry => entry.SubcontractorWorkerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
