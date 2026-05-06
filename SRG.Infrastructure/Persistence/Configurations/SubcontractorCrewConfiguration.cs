using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class SubcontractorCrewConfiguration : IEntityTypeConfiguration<SubcontractorCrew>
{
    public void Configure(EntityTypeBuilder<SubcontractorCrew> builder)
    {
        builder.ToTable("SubcontractorCrews");

        builder.HasKey(crew => crew.Id);

        builder.Property(crew => crew.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(crew => crew.CreatedAt)
            .IsRequired();

        builder.HasOne(crew => crew.Subcontractor)
            .WithMany()
            .HasForeignKey(crew => crew.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(crew => crew.CurrentForeman)
            .WithMany()
            .HasForeignKey(crew => crew.CurrentForemanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
