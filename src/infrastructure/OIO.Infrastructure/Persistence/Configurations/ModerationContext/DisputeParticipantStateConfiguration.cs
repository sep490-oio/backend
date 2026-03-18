using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Configurations.ModerationContext;

internal sealed class DisputeParticipantStateConfiguration : IEntityTypeConfiguration<DisputeParticipantState>
{
    public void Configure(EntityTypeBuilder<DisputeParticipantState> builder)
    {
        builder.ToTable("dispute_participant_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisputeId)
            .HasColumnName("dispute_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => DisputeId.From(v));

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasConversion(x => x.Value, v => UserId.From(v));

        builder.Property(x => x.LastReadMessageId)
            .HasColumnName("last_read_message_id")
            .HasConversion(
                x => x.HasValue ? x.Value.Value : (Guid?)null,
                v => v.HasValue ? DisputeMessageId.From(v.Value) : null);

        builder.Property(x => x.LastReadAt)
            .HasColumnName("last_read_at");

        builder.Property(x => x.LastSeenAt)
            .HasColumnName("last_seen_at");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();

        builder.Property(x => x.ModifiedAt)
            .HasColumnName("modified_at");

        builder.HasOne(x => x.Dispute)
            .WithMany()
            .HasForeignKey(x => x.DisputeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DisputeId, x.UserId })
            .IsUnique()
            .HasDatabaseName("idx_unique_dispute_participant_states_dispute_user");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("idx_dispute_participant_states_user");
    }
}
