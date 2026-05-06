using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class IssueItemConfiguration : IEntityTypeConfiguration<IssueItem>
{
    public void Configure(EntityTypeBuilder<IssueItem> builder)
    {
        builder.ToTable("IssueItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Quantity).HasPrecision(12, 2).IsRequired();
        builder.HasOne(item => item.Issue).WithMany(issue => issue.Items).HasForeignKey(item => item.IssueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Material).WithMany().HasForeignKey(item => item.MaterialId).OnDelete(DeleteBehavior.Restrict);
    }
}
