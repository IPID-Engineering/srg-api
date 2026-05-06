using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class SubcontractorCrewPmAccessConfiguration : IEntityTypeConfiguration<SubcontractorCrewPmAccess>
{
    public void Configure(EntityTypeBuilder<SubcontractorCrewPmAccess> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Crew)
            .WithMany(c => c.PmAccessList)
            .HasForeignKey(a => a.CrewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.PmUser)
            .WithMany()
            .HasForeignKey(a => a.PmUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.GrantedBySubcontractor)
            .WithMany()
            .HasForeignKey(a => a.GrantedBySubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CrewId, a.PmUserId })
            .IsUnique();
    }
}
