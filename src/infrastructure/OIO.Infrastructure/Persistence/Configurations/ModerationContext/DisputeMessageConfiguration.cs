using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeMessageConfiguration : IEntityTypeConfiguration<DisputeMessage>
{
    public void Configure(EntityTypeBuilder<DisputeMessage> builder)
    {
        builder.ToTable("dispute_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired();

        builder.Property(m => m.SenderId)
            .HasColumnName("sender_id")
            .IsRequired();

        builder.Property(m => m.Message)
            .HasColumnName("message")
            .IsRequired();

        builder.Property(m => m.IsInternal)
            .HasColumnName("is_internal")
            .HasDefaultValue(false);

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.HasMany(m => m.Attachments)
            .WithOne(x => x.DisputeMessage)
            .HasForeignKey(x => x.DisputeMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(m => new { m.DisputeId, m.CreatedAt, m.Id })
            .HasDatabaseName("idx_dispute_messages_dispute_created_at");
    }
}
