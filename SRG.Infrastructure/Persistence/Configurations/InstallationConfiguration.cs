using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class InstallationConfiguration : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("Installations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.CreatedAt).IsRequired();

        builder.HasOne(i => i.Section)
            .WithMany(s => s.Installations)
            .HasForeignKey(i => i.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
