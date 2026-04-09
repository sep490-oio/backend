using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeFindingConfiguration : IEntityTypeConfiguration<DisputeFinding>
{
    public void Configure(EntityTypeBuilder<DisputeFinding> builder)
    {
        builder.ToTable("dispute_findings");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeFindingId.From(v));

        builder.Property(f => f.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeId.From(v));

        builder.Property(f => f.Domain)
            .HasColumnName("domain")
            .IsRequired();

        builder.Property(f => f.AuthorUserId)
            .HasColumnName("author_user_id")
            .IsRequired();

        builder.Property(f => f.VerdictRecommendation)
            .HasColumnName("verdict_recommendation");

        builder.Property(f => f.Summary)
            .HasColumnName("summary")
            .IsRequired();

        builder.Property(f => f.FindingNote)
            .HasColumnName("finding_note");

        builder.Property(f => f.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasMany(f => f.References)
            .WithOne()
            .HasForeignKey(r => r.FindingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => f.DisputeId)
            .HasDatabaseName("idx_dispute_findings_dispute_id");
    }
}
