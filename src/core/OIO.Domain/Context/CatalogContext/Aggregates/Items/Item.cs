using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.Aggregates.Items.Events;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Domain.Context.CatalogContext.Aggregates.Items;

public sealed class Item : AggregateRoot<ItemId>, IAuditableEntity
{
    private readonly List<ItemMedia> _media = [];
    private readonly List<ItemQuestion> _questions = [];
    private readonly List<ItemModerationReview> _moderationReviews = [];
    private readonly List<Auction> _auctions = [];
    
    public UserId SellerId { get; private set; }
    public CategoryId? CategoryId { get; private set; }
    public ItemTitle Title { get; private set; }
    public string? Description { get; private set; }
    public ItemCondition Condition { get; private set; }
    public ItemStatus Status { get; private set; }
    public int Quantity { get; private set; }
    public string? Attributes { get; private set; }       // jsonb
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public int ResubmissionCount { get; private set; }
    /// <summary>
    /// Canonical flag indicating whether this item is going through the platform
    /// verification workflow (seller ships physical item to warehouse for inspection).
    /// Set at Submit/Resubmit time and used as the source of truth for downstream
    /// flows (seller items filter, inbound eligibility, etc). Auction.VerifyByPlatform
    /// is retained only as a compatibility snapshot of this field.
    /// </summary>
    public bool RequiresPlatformInspection { get; private set; }
    public UserId? AssignedAdminId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation properties
    public Category? Category { get; private set; }
    public IReadOnlyCollection<Auction> Auctions => _auctions.AsReadOnly();
    public IReadOnlyCollection<ItemMedia> Media => _media.AsReadOnly();
    public IReadOnlyCollection<ItemQuestion> Questions => _questions.AsReadOnly();
    public IReadOnlyCollection<ItemModerationReview> ModerationReviews => _moderationReviews.AsReadOnly();
    

    private Item() { }

    public static Item Create(
        DateTime nowUtc,
        UserId sellerId,
        ItemTitle title,
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
            $"{title}",
            nowUtc));

        return item;
    }

    public UnitResult<Error> Submit(bool verifyByPlatform, DateTime nowUtc)
    {
        if (_media.Count == 0)
            return AuctionErrors.Item.CannotActivate("Item must have at least one image before submission.");

        var targetStatus = verifyByPlatform ? ItemStatus.PendingVerify : ItemStatus.PendingReview;
        var result = EnsureCanTransition(targetStatus);

        if (result.IsFailure)
            return result.Error;

        SubmittedAt = nowUtc;
        RequiresPlatformInspection = verifyByPlatform;
        ChangeStatus(targetStatus, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.Submitted,
            reviewerId: SellerId,
            oldStatus: ItemStatus.Draft.Id,
            newStatus: targetStatus.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve(UserId adminId, DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Approved);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ReviewedAt = nowUtc;
        ReviewedBy = adminId;
        RejectionReason = null;
        ChangeStatus(ItemStatus.Approved, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.Approved,
            reviewerId: adminId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.Approved.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ApproveFromPlatformInspection(UserId inspectorId, DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Approved);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ReviewedAt = nowUtc;
        ReviewedBy = inspectorId;
        RejectionReason = null;
        ChangeStatus(ItemStatus.Approved, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.PlatformVerified,
            reviewerId: inspectorId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.Approved.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(UserId adminId, string reason, DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Rejected);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ReviewedAt = nowUtc;
        ReviewedBy = adminId;
        RejectionReason = reason;
        ChangeStatus(ItemStatus.Rejected, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.Rejected,
            reviewerId: adminId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.Rejected.Id,
            nowUtc: nowUtc,
            reason: reason));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RejectFromPlatformInspection(UserId inspectorId, string reason, DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Rejected);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ReviewedAt = nowUtc;
        ReviewedBy = inspectorId;
        RejectionReason = reason;
        ChangeStatus(ItemStatus.Rejected, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.PlatformRejected,
            reviewerId: inspectorId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.Rejected.Id,
            nowUtc: nowUtc,
            reason: reason));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> RequireConditionConfirmation(UserId inspectorId, DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.PendingConditionConfirmation);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ReviewedAt = nowUtc;
        ReviewedBy = inspectorId;
        RejectionReason = null;
        ChangeStatus(ItemStatus.PendingConditionConfirmation, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.ConditionConfirmationRequested,
            reviewerId: inspectorId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.PendingConditionConfirmation.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ConfirmInspectedCondition(
        UserId sellerId,
        ItemCondition condition,
        DateTime nowUtc)
    {
        var result = EnsureCanTransition(ItemStatus.Approved);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        Condition = condition;
        RejectionReason = null;
        ChangeStatus(ItemStatus.Approved, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.ConditionConfirmed,
            reviewerId: sellerId,
            oldStatus: oldStatus,
            newStatus: ItemStatus.Approved.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resubmit(bool verifyByPlatform, DateTime nowUtc)
    {
        var targetStatus = verifyByPlatform ? ItemStatus.PendingVerify : ItemStatus.PendingReview;
        var result = EnsureCanTransition(targetStatus);

        if (result.IsFailure)
            return result.Error;

        var oldStatus = Status.Id;
        ResubmissionCount++;
        SubmittedAt = nowUtc;
        RejectionReason = null;
        RequiresPlatformInspection = verifyByPlatform;
        ChangeStatus(targetStatus, nowUtc);

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.Resubmitted,
            reviewerId: SellerId,
            oldStatus: oldStatus,
            newStatus: targetStatus.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> AssignAdmin(UserId adminId, DateTime nowUtc)
    {
        if (Status != ItemStatus.Draft &&
            Status != ItemStatus.PendingReview)
            return AuctionErrors.Item.InvalidState(Status.Id, "assign admin");

        AssignedAdminId = adminId;
        ModifiedAt = nowUtc;

        _moderationReviews.Add(ItemModerationReview.Create(
            itemId: Id,
            action: ModerationAction.Assigned,
            reviewerId: adminId,
            oldStatus: Status.Id,
            newStatus: Status.Id,
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
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
        // Idempotent: only transition from Approved/Active. No-op for any other state
        // (already InAuction, terminal Sold/Removed, etc.) so callers can invoke this
        // unconditionally from auction lifecycle handlers without risking overwrites.
        if (Status != ItemStatus.Approved && Status != ItemStatus.Active)
            return UnitResult.Success<Error>();

        ChangeStatus(ItemStatus.InAuction, nowUtc);

        return UnitResult.Success<Error>();
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
        // If item is already Active or still Approved (never entered InAuction), no transition needed.
        if (Status == ItemStatus.Active || Status == ItemStatus.Approved)
            return UnitResult.Success<Error>();

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
        ItemTitle? title = null,
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
        
        RaiseDomainEvent(new ItemUpdatedEvent(
            ItemId: $"{Id}",
            Title: title?.Value,
            Description: description,
            Condition: condition?.Id,
            Quantity: quantity,
            nowUtc));

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
        
        if (upload.Info.SecureUrl is null)
            return Error.Conflict("Media.Invalid","Media upload does not have a valid URL.");

        // Validate limits using externally provided max
        var currentCount = _media.Count(m => m.ResourceType == upload.ResourceType);
        
        if (currentCount >= maxForType)
            return Error.Conflict("Media.LimitReached", $"Maximum {maxForType} {upload.ResourceType} files reached.");
        
        var order = sortOrder ?? _media.Count;
        
        // If setting as primary, unset existing primary (images only)
        if (isPrimary && upload.ResourceType == "image")
        {
            foreach (var existing in _media.Where(m => m is { IsPrimary: true }))
                existing.UnsetPrimary();
        }

        var image = ItemMedia.Create(
            nowUtc: nowUtc,
            itemId: Id, 
            resourceType: upload.ResourceType,
            isPrimary: isPrimary,
            sortOrder: order,
            info: upload.Info,
            storageRef: upload.StorageRef);
        
        _media.Add(image);
        
        ModifiedAt = nowUtc;
        
        upload.LinkToEntity(Id, nowUtc);

        RaiseDomainEvent(new MediaAddedToItemEvent(
            ItemId: $"{Id}",
            MediaId: $"{image.Id}",
            PublicId: upload.StorageRef.PublicId,
            ResourceType: upload.ResourceType,
            IsPrimary: isPrimary,
            nowUtc));

        return image;
    }

    public UnitResult<Error> RefreshMediaSnapshot(
        string oldPublicId,
        StorageRef storageRef,
        MediaInfo info,
        DateTime nowUtc)
    {
        var media = _media.FirstOrDefault(x => x.StorageRef.PublicId == oldPublicId);

        if (media is null)
            return Error.NotFound("Media.ItemMediaNotFound", $"Item media not found for public id '{oldPublicId}'.");

        media.RefreshMediaSnapshot(storageRef, info);
        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
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

        if (media.IsPrimary)
        {
            var nextImage = _media
                .OrderBy(m => m.SortOrder)
                .FirstOrDefault();

            nextImage?.SetAsPrimary();
        }

        ModifiedAt = nowUtc;
        
        RaiseDomainEvent(new MediaRemovedFromItemEvent(
            ItemId: $"{Id}",
            MediaId: $"{media.Id}",
            PublicId: media.StorageRef.PublicId,
            ResourceType: media.ResourceType, 
            nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> SetPrimaryImage(
        ItemMediaId mediaId,
        DateTime nowUtc)
    {
        var image = _media.FirstOrDefault(i => i.Id == mediaId);

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

        RaiseDomainEvent(new ItemMediaPrimarySetEvent(
            ItemId: $"{Id}",
            MediaId: $"{mediaId}",
            nowUtc));
        
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

        RaiseDomainEvent(new ItemMediaReorderedEvent(
            ItemId: $"{Id}",
            nowUtc));
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
        (Status == ItemStatus.Active || Status == ItemStatus.Approved) && _media.Count > 0;

    private UnitResult<Error> EnsureEditable()
    {
        if (!Status.IsEditable)
            return AuctionErrors.Item.InvalidState(Status.Id, "edit item");
        
        return UnitResult.Success<Error>();
    }
}
