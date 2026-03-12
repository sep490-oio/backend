using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ReviewContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.ReviewContext;

internal sealed class ReviewReportConfiguration : IEntityTypeConfiguration<ReviewReport>
{
    public void Configure(EntityTypeBuilder<ReviewReport> builder)
    {
        builder.ToTable("review_reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(r => r.ReporterId)
            .HasColumnName("reporter_id")
            .IsRequired();

        builder.ComplexProperty(r => r.Reason, reasonBuilder =>
        {
            reasonBuilder.Property(x => x.Id)
                .HasColumnName("reason")
                .HasMaxLength(50)
                .IsRequired();
        });

        builder.Property(r => r.Description)
            .HasColumnName("description");

        builder.ComplexProperty(r => r.Status, statusBuilder =>
        {
            statusBuilder.Property(s => s.Id)
                .HasColumnName("status")
                .HasMaxLength(20);
        });

        builder.Property(r => r.ReviewedBy)
            .HasColumnName("reviewed_by");

        builder.Property(r => r.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
    }
}
