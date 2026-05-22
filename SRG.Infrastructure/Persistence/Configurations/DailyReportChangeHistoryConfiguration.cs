using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class DailyReportChangeHistoryConfiguration : IEntityTypeConfiguration<DailyReportChangeHistory>
{
    public void Configure(EntityTypeBuilder<DailyReportChangeHistory> builder)
    {
        builder.HasKey(h => h.Id);
        
        builder.Property(h => h.EntryType).HasMaxLength(50).IsRequired();
        builder.Property(h => h.ChangeType).HasMaxLength(20).IsRequired();
        builder.Property(h => h.OldValues).HasColumnType("text");
        builder.Property(h => h.NewValues).HasColumnType("text");
        
        builder.HasOne(h => h.DailyReport)
            .WithMany(r => r.ChangeHistory)
            .HasForeignKey(h => h.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(h => h.ChangedBy)
            .WithMany()
            .HasForeignKey(h => h.ChangedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.Property(h => h.ChangedByEmail).HasMaxLength(256);
            
        builder.HasIndex(h => h.DailyReportId);
        builder.HasIndex(h => new { h.DailyReportId, h.EntryId });
    }
}
