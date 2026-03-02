using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class Item : AggregateRoot<ItemId>, IAuditableEntity
{
    private readonly List<ItemImage> _images = [];
    private readonly List<ItemQuestion> _questions = [];
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid OwnerId { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public IReadOnlyList<ItemImage> Images => _images;
    public IReadOnlyList<ItemQuestion> Questions => _questions;
    private Item() { }

    public static Item Create(string name, string description, Guid ownerId, DateTime now)
    {
        return new Item 
        { 
            Id = ItemId.From(Guid.CreateVersion7()),
            Name = name,
            Description = description,
            OwnerId = ownerId,
            CreatedAt = now
        };
    }

    public UnitResult<Error> AddImage(string url, bool isMain)
    {
        if (_images.Count >= 10) return AuctionErrors.Item.MaxImagesReached;
        
        if (isMain) _images.ForEach(x => x.SetAsMain(false));
        
        _images.Add(new ItemImage(ItemImageId.From(Guid.CreateVersion7()), Id, url, isMain, _images.Count));
        return UnitResult.Success<Error>();
    }
    public void AskQuestion(Guid userId, string content, DateTime now)
    {
        _questions.Add(new ItemQuestion(ItemQuestionId.From(Guid.CreateVersion7()), Id, userId, content, now));
    }
}