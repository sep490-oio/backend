using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeFindingReferenceConfiguration : IEntityTypeConfiguration<DisputeFindingReference>
{
    public void Configure(EntityTypeBuilder<DisputeFindingReference> builder)
    {
        builder.ToTable("dispute_finding_references");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(r => r.FindingId)
            .HasColumnName("finding_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeFindingId.From(v));

        builder.Property(r => r.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.TargetId)
            .HasColumnName("target_id")
            .IsRequired();

        builder.Property(r => r.LabelSnapshot)
            .HasColumnName("label_snapshot")
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasIndex(r => r.FindingId)
            .HasDatabaseName("idx_dispute_finding_references_finding_id");
    }
}
