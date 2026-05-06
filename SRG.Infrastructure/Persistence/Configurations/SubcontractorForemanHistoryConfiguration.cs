using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class SubcontractorForemanHistoryConfiguration : IEntityTypeConfiguration<SubcontractorForemanHistory>
{
    public void Configure(EntityTypeBuilder<SubcontractorForemanHistory> builder)
    {
        builder.ToTable("SubcontractorForemanHistory");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.StartDate)
            .IsRequired();

        builder.Property(history => history.CreatedAt)
            .IsRequired();

        builder.HasOne(history => history.Crew)
            .WithMany(crew => crew.ForemanHistory)
            .HasForeignKey(history => history.CrewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(history => history.Foreman)
            .WithMany(worker => worker.ForemanHistory)
            .HasForeignKey(history => history.ForemanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.CrewId, history.EndDate })
            .HasFilter("\"EndDate\" IS NULL")
            .IsUnique();
    }
}
