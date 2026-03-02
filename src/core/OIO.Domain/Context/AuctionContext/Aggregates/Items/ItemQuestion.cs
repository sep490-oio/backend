using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class ItemQuestion : BaseEntity<ItemQuestionId>, IAuditableEntity
{
    public ItemId ItemId { get; private set; }
    public Guid RequesterId { get; private set; }
    public string Content { get; private set; }
    public string? Answer { get; private set; }
    public DateTime? AnsweredAt { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    internal ItemQuestion(ItemQuestionId id, ItemId itemId, Guid requesterId, string content, DateTime now) : base(id)
    {
        ItemId = itemId;
        RequesterId = requesterId;
        Content = content;
        CreatedAt = now;
    }

    public void Reply(string answer, DateTime now)
    {
        Answer = answer;
        AnsweredAt = now;
        ModifiedAt = now;
    }
}