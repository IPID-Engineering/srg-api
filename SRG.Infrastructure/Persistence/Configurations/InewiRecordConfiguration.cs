using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class InewiRecordConfiguration : IEntityTypeConfiguration<InewiRecord>
{
    public void Configure(EntityTypeBuilder<InewiRecord> builder)
    {
        builder.ToTable("InewiRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.WorkerName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Date)
            .IsRequired();

        builder.Property(r => r.Hours)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(r => r.SourceFileName)
            .HasMaxLength(500);

        builder.Property(r => r.ImportedAt)
            .IsRequired();

        builder.HasOne(r => r.Subcontractor)
            .WithMany()
            .HasForeignKey(r => r.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ImportedBy)
            .WithMany()
            .HasForeignKey(r => r.ImportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.SubcontractorId, r.Date, r.WorkerName })
            .IsUnique();
    }
}
