using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AssistantContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.AssistantContext;

internal sealed class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("assistant_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConversationId)
            .HasColumnName("conversation_id")
            .IsRequired();

        builder.Property(m => m.Sender)
            .HasColumnName("sender")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(m => m.Content)
            .HasColumnName("content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(m => m.Citations)
            .HasColumnName("citations")
            .HasColumnType("jsonb");

        builder.Property(m => m.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb");

        builder.Property(m => m.TokenUsage)
            .HasColumnName("token_usage");

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt })
            .HasDatabaseName("idx_assistant_messages_conversation_created");
    }
}
