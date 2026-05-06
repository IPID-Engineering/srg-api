using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(120).IsRequired();
        builder.Property(log => log.EntityName).HasMaxLength(160).IsRequired();
        builder.Property(log => log.Changes).HasColumnType("jsonb").IsRequired();
        builder.Property(log => log.CreatedAt).IsRequired();
        builder.HasIndex(log => log.UserId);
        builder.HasIndex(log => log.Action);
        builder.HasIndex(log => log.EntityName);
        builder.HasIndex(log => log.CreatedAt);
    }
}
