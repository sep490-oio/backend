using OIO.Domain.Context.AuctionContext.Aggregates.Items.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class Item : AggregateRoot<ItemId>, IAuditableEntity
{
    private readonly List<ItemImage> _images = [];
    private readonly List<ItemQuestion> _questions = [];

    public UserId SellerId { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public string Title { get; private set; }
    public string? Description { get; private set; }
    public ItemCondition Condition { get; private set; }
    public ItemStatus Status { get; private set; }
    public int Quantity { get; private set; }
    public string? Attributes { get; private set; }  // jsonb stored as raw JSON string

    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public IReadOnlyCollection<ItemImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ItemQuestion> Questions => _questions.AsReadOnly();

    private Item() { }

    public static Item Create(
        UserId sellerId,
        string title,
        ItemCondition condition,
        string? description = null,
        CategoryId? categoryId = null,
        int quantity = 1,
        string? attributes = null,
        DateTime? now = null)
    {
        var item = new Item
        {
            Id = ItemId.From(Guid.CreateVersion7()),
            SellerId = sellerId,
            Title = title,
            Condition = condition,
            Description = description,
            CategoryId = categoryId,
            Quantity = quantity,
            Attributes = attributes,
            Status = ItemStatus.Draft,
            CreatedAt = now ?? DateTime.UtcNow
        };

        item.RaiseDomainEvent(new ItemCreatedEvent(
            item.Id.Value.ToString(),
            sellerId.Value.ToString(),
            now ?? DateTime.UtcNow));

        return item;
    }

    public void Update(
        string title,
        string? description,
        ItemCondition condition,
        CategoryId? categoryId,
        int quantity,
        string? attributes,
        DateTime now)
    {
        Title = title;
        Description = description;
        Condition = condition;
        CategoryId = categoryId;
        Quantity = quantity;
        Attributes = attributes;
        ModifiedAt = now;
    }

    public void Activate(DateTime now)
    {
        if (Status != ItemStatus.Draft)
            return;

        Status = ItemStatus.Active;
        ModifiedAt = now;
    }

    public void MarkInAuction(DateTime now)
    {
        Status = ItemStatus.InAuction;
        ModifiedAt = now;
    }

    public void MarkSold(DateTime now)
    {
        Status = ItemStatus.Sold;
        ModifiedAt = now;
    }

    public void Remove(DateTime now)
    {
        Status = ItemStatus.Removed;
        ModifiedAt = now;
    }
}