using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OIO.Domain.Context.AssistantContext.Aggregates;

namespace OIO.Infrastructure.Persistence.Configurations.AssistantContext;

internal sealed class AssistantConversationConfiguration : IEntityTypeConfiguration<AssistantConversation>
{
    public void Configure(EntityTypeBuilder<AssistantConversation> builder)
    {
        builder.ToTable("assistant_conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .HasColumnName("user_id")
            .IsRequired(false);

        builder.Property(c => c.RoleContext)
            .HasColumnName("role_context")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(c => c.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(c => c.ModifiedAt)
            .HasColumnName("modified_at");

        builder.Property(c => c.LastMessageAt)
            .HasColumnName("last_message_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.UserId, c.LastMessageAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_assistant_conversations_user_last_message");
    }
}
