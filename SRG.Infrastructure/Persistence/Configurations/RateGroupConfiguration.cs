using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class RateGroupConfiguration : IEntityTypeConfiguration<RateGroup>
{
    public void Configure(EntityTypeBuilder<RateGroup> builder)
    {
        builder.HasKey(rg => rg.Id);
        
        builder.Property(rg => rg.Name)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(rg => rg.HourlyRate)
            .HasPrecision(10, 2);
            
        builder.Property(rg => rg.HourlyCost)
            .HasPrecision(10, 2);
        
        builder.HasOne(rg => rg.Subcontractor)
            .WithMany()
            .HasForeignKey(rg => rg.SubcontractorId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasMany(rg => rg.Workers)
            .WithOne(w => w.RateGroup)
            .HasForeignKey(w => w.RateGroupId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
