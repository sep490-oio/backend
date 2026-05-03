using OIO.Domain.Context.AssistantContext.Enums;
using OIO.Domain.Context.AssistantContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AssistantContext.Aggregates;

public sealed class AssistantMessage : BaseEntity<AssistantMessageId>, ICreatedAtEntity
{
    public AssistantConversationId ConversationId { get; private set; }
    public AssistantSender Sender { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public string? Citations { get; private set; }    // jsonb array
    public string? Metadata { get; private set; }     // jsonb
    public int? TokenUsage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public AssistantConversation? Conversation { get; private set; }

    private AssistantMessage() { }

    public static AssistantMessage Create(
        AssistantConversationId conversationId,
        AssistantSender sender,
        string content,
        string? citations = null,
        string? metadata = null,
        int? tokenUsage = null)
    {
        return new AssistantMessage
        {
            Id = AssistantMessageId.From(Guid.NewGuid()),
            ConversationId = conversationId,
            Sender = sender,
            Content = content ?? string.Empty,
            Citations = citations,
            Metadata = metadata,
            TokenUsage = tokenUsage,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
