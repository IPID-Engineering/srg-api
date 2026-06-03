using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class InewiIntegrationSettingsConfiguration : IEntityTypeConfiguration<InewiIntegrationSettings>
{
    public void Configure(EntityTypeBuilder<InewiIntegrationSettings> builder)
    {
        builder.ToTable("InewiIntegrationSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.EncryptedPassword)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.AccessToken)
            .HasMaxLength(2048);

        builder.Property(x => x.LastError)
            .HasMaxLength(2048);

        builder.HasOne(x => x.Subcontractor)
            .WithOne()
            .HasForeignKey<InewiIntegrationSettings>(x => x.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ConfiguredBy)
            .WithMany()
            .HasForeignKey(x => x.ConfiguredById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SubcontractorId)
            .IsUnique();
    }
}
