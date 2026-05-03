using OIO.Domain.Context.AssistantContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using DomainUserId = OIO.Domain.Context.UserContext.ValueObjects.Ids.UserId;

namespace OIO.Domain.Context.AssistantContext.Aggregates;

public sealed class AssistantConversation : AggregateRoot<AssistantConversationId>, IAuditableEntity
{
    private readonly List<AssistantMessage> _messages = [];

    public DomainUserId? UserId { get; private set; }
    public string RoleContext { get; private set; } = "guest";
    public string Title { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public DateTime LastMessageAt { get; private set; }

    public IReadOnlyCollection<AssistantMessage> Messages => _messages.AsReadOnly();

    private AssistantConversation() { }

    public static AssistantConversation Create(Guid? userId, string roleContext, string title)
    {
        var now = DateTime.UtcNow;
        return new AssistantConversation
        {
            Id = AssistantConversationId.From(Guid.NewGuid()),
            UserId = userId.HasValue ? DomainUserId.From(userId.Value) : null,
            RoleContext = string.IsNullOrWhiteSpace(roleContext) ? "guest" : roleContext.ToLowerInvariant(),
            Title = string.IsNullOrWhiteSpace(title) ? "New conversation" : title.Trim(),
            CreatedAt = now,
            LastMessageAt = now,
        };
    }

    public void TouchLastMessage(DateTime at)
    {
        LastMessageAt = at;
        ModifiedAt = at;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return;
        Title = title.Trim();
        ModifiedAt = DateTime.UtcNow;
    }
}
