using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SRG.Domain.Entities;

namespace SRG.Infrastructure.Persistence.Configurations;

public class DailyReportCommentConfiguration : IEntityTypeConfiguration<DailyReportComment>
{
    public void Configure(EntityTypeBuilder<DailyReportComment> builder)
    {
        builder.ToTable("daily_report_comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.DailyReportId).HasColumnName("daily_report_id").IsRequired();
        builder.Property(c => c.Section).HasColumnName("section").IsRequired();
        builder.Property(c => c.RecordId).HasColumnName("record_id");
        builder.Property(c => c.AuthorId).HasColumnName("author_id");
        builder.Property(c => c.SubcontractorWorkerId).HasColumnName("subcontractor_worker_id");
        builder.Property(c => c.AuthorEmail).HasColumnName("author_email").HasMaxLength(256);
        builder.Property(c => c.AuthorRole).HasColumnName("author_role").HasMaxLength(64);
        builder.Property(c => c.Content).HasColumnName("content").IsRequired();
        builder.Property(c => c.ParentCommentId).HasColumnName("parent_comment_id");
        builder.Property(c => c.IsResolved).HasColumnName("is_resolved");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at");

        builder.HasOne(c => c.DailyReport)
            .WithMany(d => d.Comments)
            .HasForeignKey(c => c.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Author)
            .WithMany()
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(c => c.SubcontractorWorker)
            .WithMany()
            .HasForeignKey(c => c.SubcontractorWorkerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.DailyReportId);
        builder.HasIndex(c => c.AuthorId);
        builder.HasIndex(c => c.SubcontractorWorkerId);
    }
}
