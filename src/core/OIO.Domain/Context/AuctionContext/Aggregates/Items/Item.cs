using CSharpFunctionalExtensions;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class Item : AggregateRoot<ItemId>, IAuditableEntity
{
    private readonly List<ItemMedia> _media = [];
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

    public IReadOnlyCollection<ItemMedia> Media => _media.AsReadOnly();
    public IReadOnlyCollection<ItemQuestion> Questions => _questions.AsReadOnly();
    

    private Item() { }

    public static Item Create(
        DateTime nowUtc,
        UserId sellerId,
        string title,
        ItemCondition condition,
        string? description = null,
        CategoryId? categoryId = null,
        int quantity = 1,
        string? attributes = null)
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
            CreatedAt = nowUtc
        };

        item.RaiseDomainEvent(new ItemCreatedEvent(
            $"{item.Id}",
            $"{sellerId}",
            title,
            nowUtc));

        return item;
    }

    public UnitResult<Error> Activate(DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Active);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        if (_media.Count == 0)
            return AuctionErrors.Item.CannotActivate("Item must have at least one image before activation.");
        
        ChangeStatus(ItemStatus.Active, nowUtc);
        
        return result;
    }

    public UnitResult<Error> MarkInAuction(DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.InAuction);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        
        ChangeStatus(ItemStatus.InAuction, nowUtc);
        
        return result;
    }
    
    public UnitResult<Error> MarkSold(DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Sold);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        
        ChangeStatus(ItemStatus.Sold, nowUtc);
        
        return result;
    }
    
    public UnitResult<Error> ReturnToActive(DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Active);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        
        ChangeStatus(ItemStatus.Active, nowUtc);
        
        return result;
    }

    public UnitResult<Error> Remove(DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Removed);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        ChangeStatus(ItemStatus.Removed, nowUtc);
        
        return result;
    }
    
    private void ChangeStatus(
        ItemStatus newStatus,
        DateTime nowUtc)
    {
        var oldStatus = Status;
        Status = newStatus;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new ItemStatusChangedEvent(
            $"{Id}", 
            oldStatus.Id,
            newStatus.Id,
            nowUtc));
    }
    
    private UnitResult<Error> EnsureCanTransition(ItemStatus target)
    {
        return !Status.CanTransitionTo(target) ? 
            AuctionErrors.Item.InvalidState(Status.Id, $"transition to {target.Id}") :
            UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> Update(
        DateTime nowUtc,
        string? title = null,
        string? description = null,
        ItemCondition? condition = null,
        CategoryId? categoryId = null,
        int? quantity = null,
        string? attributes = null)
    {
        var ensureResult = EnsureEditable();

        if (ensureResult.IsFailure)
        {
            return ensureResult;
        }

        var check = Item
            .Check(isInvariant: true)
            .Field(title)
            .WhenHasValue(x => x
                .NotWhiteSpace()
                .MaxLength(App.Constraint.Item.TitleMaxLength))
            .Field(quantity).WhenHasValue(x => x.NonNegative());

        var resultCheck = check.ToUnitResult();

        if (resultCheck.IsFailure)
        {
            return resultCheck.Error;
        }
       
        Title = title ?? Title;
        Description = description ?? Description;
        Condition = condition ?? Condition;
        CategoryId = categoryId ?? CategoryId;
        Attributes = attributes ?? Attributes;
        Quantity = quantity ?? Quantity;

        ModifiedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
    
    public Result<ItemMedia, Error> AddMedia(
        DateTime nowUtc,
        MediaUpload upload,
        bool isPrimary,
        int maxForType,
        int? sortOrder = null)
    {
        var ensureResult = EnsureEditable();

        if (ensureResult.IsFailure)
        {
            return ensureResult.Error;
        }

        //TODO: bring these media error to MediaErrors
        if (!upload.IsConfirmed)
        {
            return Error.Conflict("Media.NotConfirm","Media upload is not confirmed yet.");
        }
        
        if (upload.SecureUrl is null)
            return Error.Conflict("Media.Invalid","Media upload does not have a valid URL.");

        // Validate limits using externally provided max
        var currentCount = _media.Count(m => m.ResourceType == upload.ResourceType);
        
        if (currentCount >= maxForType)
            return Error.Conflict("Media.LimitReached", $"Maximum {maxForType} {upload.ResourceType} files reached.");
        
        var order = sortOrder ?? _media.Count;
        
        // If setting as primary, unset existing primary (images only)
        if (isPrimary && upload.ResourceType == "image")
        {
            foreach (var existing in _media.Where(m => m is { IsImage: true, IsPrimary: true }))
                existing.UnsetPrimary();
        }

        var image = ItemMedia.Create(
            nowUtc: nowUtc,
            itemId: Id, 
            url: upload.SecureUrl,
            publicId: upload.PublicId,
            resourceType: upload.ResourceType,
            isPrimary: isPrimary,
            sortOrder: order,
            fileName: upload.FileName,
            bytes: upload.Bytes,
            format: upload.Format,
            width: upload.Width,
            height: upload.Height,
            durationSeconds: upload.DurationSeconds);
        
        _media.Add(image);
        
        ModifiedAt = nowUtc;
        
        upload.LinkToEntity(Id.Value, nowUtc);

        return image;
    }
    
    public UnitResult<Error> RemoveMedia(
        ItemMediaId mediaId,
        DateTime nowUtc)
    {
        var ensureResult = EnsureEditable();

        if (ensureResult.IsFailure)
        {
            return ensureResult;
        }

        var media = _media.FirstOrDefault(i => i.Id == mediaId);
        
        if (media is null)
        {
            return AuctionErrors.Item.MediaNotFound(mediaId);
        }
        
        _media.Remove(media);

        if (media.IsPrimary && media.IsImage)
        {
            var nextImage = _media
                .Where(m => m.IsImage)
                .OrderBy(m => m.SortOrder)
                .FirstOrDefault();

            nextImage?.SetAsPrimary();
        }

        ModifiedAt = nowUtc;
        
        RaiseDomainEvent(new MediaRemovedFromItemEvent(
            ItemId: $"{Id}",
            MediaId: $"{media.Id}",
            PublicId: media.PublicId,
            ResourceType: media.ResourceType, 
            nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> SetPrimaryImage(
        ItemMediaId mediaId,
        DateTime nowUtc)
    {
        var image = _media.FirstOrDefault(i => i.Id == mediaId && i.IsImage);

        if (image is null)
        {
            return AuctionErrors.Item.MediaNotFound(mediaId);
        }

        foreach (var img in _media.Where(img => img.Id != mediaId))
        {
            img.UnsetPrimary();
        }    
        
        image.SetAsPrimary();
        ModifiedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
    
    public void ReorderMedia(
        DateTime nowUtc,
        IReadOnlyList<ItemMediaId> orderedMediaIds)
    {
        for (var i = 0; i < orderedMediaIds.Count; i++)
        {
            var media = _media.FirstOrDefault(m => m.Id == orderedMediaIds[i]);
            media?.Reorder(i);
        }

        ModifiedAt = nowUtc;
    }
    
    public Result<ItemQuestion, Error> AskQuestion(
        UserId askerId,
        string question,
        int maxQuestion,
        DateTime nowUtc)
    {
        if (Status == ItemStatus.Draft || Status == ItemStatus.Removed)
            return AuctionErrors.Item.InvalidState(Status.Id, "ask question");

        if (askerId == SellerId)
            return AuctionErrors.Item.AskOwnItem(Id);
        
        if (_questions.Count >= maxQuestion)
            return AuctionErrors.Item.QuestionLimitReached(Id, maxQuestion);

        var itemQuestion = ItemQuestion.Create(Id, askerId, question, nowUtc);
        
        _questions.Add(itemQuestion);

        RaiseDomainEvent(new ItemQuestionAskedEvent(
            $"{Id}", 
            $"{itemQuestion.Id}",
            $"{askerId}",
            nowUtc));

        return itemQuestion;
    }
    
    public UnitResult<Error> AnswerQuestion(
        ItemQuestionId questionId,
        string answer,
        DateTime nowUtc)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);

        if (question is null)
        {
            return AuctionErrors.Item.QuestionNotFound(questionId);
        }

        if (question.IsAnswered)
            return AuctionErrors.Item.QuestionAnswered;

        question.AnswerQuestion(answer, nowUtc);

        RaiseDomainEvent(new ItemQuestionAnsweredEvent(
            $"{Id}",
            $"{questionId}",
            $"{question.AskerId}",
            nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    public bool IsAvailableForAuction =>
        Status == ItemStatus.Active && _media.Count > 0;
    
    public IReadOnlyList<ItemMedia> Images => _media.Where(m => m.IsImage).ToList();
    public IReadOnlyList<ItemMedia> Videos => _media.Where(m => m.IsVideo).ToList();
    public IReadOnlyList<ItemMedia> Documents => _media.Where(m => m.IsDocument).ToList();
    public ItemMedia? PrimaryImage => _media.FirstOrDefault(m => m is { IsImage: true, IsPrimary: true });
    
    private UnitResult<Error> EnsureEditable()
    {
        if (Status != ItemStatus.Draft && Status != ItemStatus.Active)
            return AuctionErrors.Item.InvalidState(Status.Id, "edit item");
        
        return UnitResult.Success<Error>();
    }
}