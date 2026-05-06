using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class CrewConfiguration : IEntityTypeConfiguration<Crew>
{
    public void Configure(EntityTypeBuilder<Crew> builder)
    {
        builder.ToTable("Crews");

        builder.HasKey(crew => crew.Id);

        builder.Property(crew => crew.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(crew => crew.CreatedAt)
            .IsRequired();

        builder.HasOne(crew => crew.Project)
            .WithMany(project => project.Crews)
            .HasForeignKey(crew => crew.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(crew => crew.CreatedBy)
            .WithMany()
            .HasForeignKey(crew => crew.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
