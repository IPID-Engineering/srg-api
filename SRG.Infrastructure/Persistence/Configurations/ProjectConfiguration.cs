using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");

        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2000);

        builder.Property(project => project.CreatedAt)
            .IsRequired();

        builder.HasOne(project => project.CreatedBy)
            .WithMany()
            .HasForeignKey(project => project.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
