using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class CrewAccessConfiguration : IEntityTypeConfiguration<CrewAccess>
{
    public void Configure(EntityTypeBuilder<CrewAccess> builder)
    {
        builder.ToTable("CrewAccessList");

        builder.HasKey(ca => ca.Id);

        builder.HasOne(ca => ca.Crew)
            .WithMany(c => c.AccessList)
            .HasForeignKey(ca => ca.CrewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.User)
            .WithMany()
            .HasForeignKey(ca => ca.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.AssignedBy)
            .WithMany()
            .HasForeignKey(ca => ca.AssignedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ca => new { ca.CrewId, ca.UserId }).IsUnique();
    }
}
