using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class ProjectSubcontractorConfiguration : IEntityTypeConfiguration<ProjectSubcontractor>
{
    public void Configure(EntityTypeBuilder<ProjectSubcontractor> builder)
    {
        builder.ToTable("ProjectSubcontractors");

        builder.HasKey(assignment => assignment.Id);

        builder.HasIndex(assignment => new { assignment.ProjectId, assignment.SubcontractorId })
            .IsUnique();

        builder.HasOne(assignment => assignment.Project)
            .WithMany(project => project.ProjectSubcontractors)
            .HasForeignKey(assignment => assignment.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(assignment => assignment.Subcontractor)
            .WithMany()
            .HasForeignKey(assignment => assignment.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
