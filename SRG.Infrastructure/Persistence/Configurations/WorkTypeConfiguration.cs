using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class WorkTypeConfiguration : IEntityTypeConfiguration<WorkType>
{
    public void Configure(EntityTypeBuilder<WorkType> builder)
    {
        builder.ToTable("WorkTypes");
        builder.HasKey(workType => workType.Id);
        builder.Property(workType => workType.Code).HasMaxLength(50).IsRequired();
        builder.Property(workType => workType.Name).HasMaxLength(200).IsRequired();
        builder.Property(workType => workType.Description).HasMaxLength(1000);
        builder.Property(workType => workType.Unit).HasMaxLength(20).HasDefaultValue("szt");
        builder.Property(workType => workType.IsActive).IsRequired();
        builder.Property(workType => workType.CreatedAt).IsRequired();
        builder.HasIndex(workType => workType.Code).IsUnique();
    }
}
