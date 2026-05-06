using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("Materials");
        builder.HasKey(material => material.Id);
        builder.Property(material => material.Name).HasMaxLength(200).IsRequired();
        builder.Property(material => material.Unit).HasMaxLength(32).IsRequired();
        builder.Property(material => material.CreatedAt).IsRequired();
        builder.HasOne(material => material.Category)
            .WithMany(category => category.Materials)
            .HasForeignKey(material => material.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
