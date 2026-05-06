using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WorkerConfiguration : IEntityTypeConfiguration<Worker>
{
    public void Configure(EntityTypeBuilder<Worker> builder)
    {
        builder.ToTable("Workers");

        builder.HasKey(person => person.Id);

        builder.Property(person => person.FirstName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(person => person.LastName)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasOne(person => person.Crew)
            .WithMany(crew => crew.Worker)
            .HasForeignKey(person => person.CrewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(person => person.CreatedBy)
            .WithMany()
            .HasForeignKey(person => person.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(person => person.Team)
            .WithMany(team => team.Worker)
            .HasForeignKey(person => person.TeamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
