using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(160).IsRequired();
        builder.Property(category => category.FamilyCode).HasMaxLength(10);
        builder.Property(category => category.SubFamilyCode).HasMaxLength(10);
        builder.Property(category => category.CreatedAt).IsRequired();

        builder.HasIndex(category => new { category.FamilyCode, category.SubFamilyCode });

        builder.HasOne(category => category.ParentCategory)
            .WithMany(category => category.SubCategories)
            .HasForeignKey(category => category.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
