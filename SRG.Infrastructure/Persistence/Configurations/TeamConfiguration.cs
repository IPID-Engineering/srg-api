using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(team => team.Id);

        builder.Property(team => team.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne(team => team.Crew)
            .WithMany(crew => crew.Teams)
            .HasForeignKey(team => team.CrewId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
