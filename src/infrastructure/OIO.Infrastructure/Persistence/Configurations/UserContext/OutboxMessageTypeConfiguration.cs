using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Infrastructure.Outbox;

namespace OIO.Infrastructure.Persistence.Configurations.UserContext;

internal sealed class OutboxMessageTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(outboxMessage => outboxMessage.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(outboxMessage => outboxMessage.Type)
            .HasMaxLength(255)
            .IsRequired()
            .HasColumnName("type");

        builder.Property(outboxMessage => outboxMessage.Content)
            .HasColumnType("jsonb")
            .IsRequired()
            .HasColumnName("content");

        builder
            .Property(outboxMessage => outboxMessage.OccurredAt)
            .IsRequired()
            .HasColumnName("occurred_at");

        builder.Property(o => o.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(o => o.Error)
            .HasColumnName("error");

        builder.Property(o => o.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnName("attempt_count");

        // Partial index for the processor query: only includes messages eligible for processing.
        // The attempt_count filter MUST match OutboxConstants.MaxAttemptsIndexFilter (validated at startup).
        builder
            .HasIndex(o => new { o.OccurredAt })
            .HasDatabaseName("idx_outbox_messages_unprocessed")
            .HasFilter($"processed_at IS NULL AND attempt_count < {OutboxConstants.MaxAttemptsIndexFilter}");

        builder
            .HasIndex(om => new { om.OccurredAt, om.Id })
            .HasDatabaseName("idx_outbox_cleanup")
            .HasFilter("processed_at IS NOT NULL AND error IS NULL");

        // Index for poison message cleanup
        builder
            .HasIndex(o => new { o.OccurredAt, o.Id })
            .HasDatabaseName("idx_outbox_cleanup_poison")
            .HasFilter($"processed_at IS NULL AND attempt_count >= {OutboxConstants.MaxAttemptsIndexFilter}");

        // Index for error message cleanup (deserialization failures)
        builder
            .HasIndex(o => new { o.OccurredAt, o.Id })
            .HasDatabaseName("idx_outbox_cleanup_error")
            .HasFilter("processed_at IS NOT NULL AND error IS NOT NULL");
    }
}