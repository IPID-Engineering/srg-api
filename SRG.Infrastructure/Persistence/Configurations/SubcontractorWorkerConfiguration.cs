using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class SubcontractorWorkerConfiguration : IEntityTypeConfiguration<SubcontractorWorker>
{
    public void Configure(EntityTypeBuilder<SubcontractorWorker> builder)
    {
        builder.ToTable("SubcontractorWorkers");

        builder.HasKey(worker => worker.Id);

        builder.Property(worker => worker.FirstName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(worker => worker.LastName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(worker => worker.CreatedAt)
            .IsRequired();

        builder.HasOne(worker => worker.Subcontractor)
            .WithMany()
            .HasForeignKey(worker => worker.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(worker => worker.Crew)
            .WithMany(crew => crew.Workers)
            .HasForeignKey(worker => worker.CrewId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(worker => worker.InewiEmployeeId)
            .HasMaxLength(50);
    }
}
