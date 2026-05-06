using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.ToTable("Sections");

        builder.HasKey(section => section.Id);

        builder.Property(section => section.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(section => section.Project)
            .WithMany(project => project.Sections)
            .HasForeignKey(section => section.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
