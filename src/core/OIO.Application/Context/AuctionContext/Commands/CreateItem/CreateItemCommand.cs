using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Categories;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Commands.CreateItem;

public sealed record CreateItemCommand(
    string Title,
    string Condition,
    Guid? CategoryId = null,
    string? Description = null,
    int Quantity = 1,
    string? Attributes = null,
    IReadOnlyList<MediaAttachment>? Media = null) : ICommand<ItemDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var checkError = CreateItemCommand
            .Check()
            .WithOwnerName("CreateItem")
            .Field(Title)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.Item.TitleMaxLength)
            .Field(Condition)
            .NotWhiteSpace()
            .InSet(ItemCondition.All.Select(x => x.Id))
            .Field(CategoryId)
            .WhenHasValue(x => x.NotEmptyGuid())
            .Field(Description)
            .WhenHasValue(x => x.NotWhiteSpace())
            .Field(Quantity)
            .NotDefault()
            .Positive()
            .Field(Attributes)
            .WhenHasValue(x => x.NotWhiteSpace())
            .ToViolationsError();

        if (Media is null) 
            return checkError;
        
        for (var i = 0; i < Media.Count; i++)
        {
            var imageCheckError = Media[i]
                .Validate();
                
            checkError.Add(imageCheckError);
        }

        return checkError;
    }
}

/// <summary>
/// References a previously uploaded and confirmed image.
/// </summary>
public sealed record MediaAttachment(
    Guid MediaUploadId,
    bool IsPrimary = false,
    int SortOrder = 0) : IHasValidate
{
    public ViolationsError Validate()
    {
        return MediaAttachment.Check()
            .Field(MediaUploadId)
            .NotEmptyGuid();
    }
}

internal sealed class CreateItemCommandHandler
    : ICommandHandler<CreateItemCommand, ItemDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly ILogger<CreateItemCommandHandler> _logger;

    public CreateItemCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService,
        ILogger<CreateItemCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
        _logger = logger;
    }

    public async Task<Result<ItemDto, Error>> Handle(
        CreateItemCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        
        CategoryId? categoryId = null;
        if (request.CategoryId.HasValue)
        {
            categoryId = CategoryId.From(request.CategoryId.Value);
            
            var categoryExists = await _dbContext.Set<Category>()
                .AnyAsync(x => x.Id == categoryId, cancellationToken: cancellationToken);

            if (!categoryExists)
                return AuctionErrors.Category.NotFound(categoryId.Value);
        }
        
        List<MediaUpload>? mediaUploads = null;
        if (request.Media is { Count: > 0 })
        {
            var pendingIds = request.Media.Select(i => MediaUploadId.From(i.MediaUploadId)).ToList();

            mediaUploads = await _dbContext.Set<MediaUpload>()
                .Where(p => pendingIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            // Validate all uploads exist and belong to current user
            var validationError = await ValidatePendingUploadsAsync(
                mediaUploads, 
                pendingIds,
                _currentUser.UserId,
                cancellationToken);

            if (validationError is not null)
                return validationError;
        }
        
        var ( _, isFailure, title, error) = ItemTitle.Create(request.Title);

        if (isFailure)
        {
            return error;
        }


        var condition = ItemCondition.FromId(request.Condition);

        var item = Item.Create(
            sellerId: _currentUser.UserId,
            title: title,
            condition: condition.Value,
            nowUtc: nowUtc,
            categoryId: categoryId,
            description: request.Description,
            quantity: request.Quantity,
            attributes: request.Attributes);
        
        // Link images from pending uploads
        if (mediaUploads is not null && request.Media is not null)
        {
            foreach (var mediaReq in request.Media.OrderBy(i => i.SortOrder))
            {
                var mediaUploadId = MediaUploadId.From(mediaReq.MediaUploadId);
                
                var upload = mediaUploads.First(p => p.Id == mediaUploadId);

                var maxForType = _contextRegistry.GetMaxForEntityMedia("item", upload.ResourceType);
                
                // Add image to item domain
                item.AddMedia(
                    nowUtc: nowUtc,
                    upload: upload,
                    isPrimary: mediaReq.IsPrimary,
                    maxForType: maxForType,
                    sortOrder: mediaReq.SortOrder);
            }
        }

        _dbContext.Insert(item);

        if (mediaUploads is not null)
        {
            foreach (var upload in mediaUploads)
                await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return item.ToDto();
    }

    private async Task<Error?> ValidatePendingUploadsAsync(
        List<MediaUpload> found,
        List<MediaUploadId> requestedIds,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        // Check all requested uploads were found
        var foundIds = found.Select(p => p.Id).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();

        if (missingIds.Count > 0)
        {
            _logger.LogWarning("Media uploads not found: {MissingIds}", string.Join(", ", missingIds));
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        // Check ownership
        var notOwned = found.Where(p => p.UserId != userId).ToList();
        if (notOwned.Count > 0)
        {
                _logger.LogWarning("Media uploads not owned by user {UserId}: {NotOwnedIds}",
                    userId, string.Join(", ", notOwned.Select(p => p.Id)));
            return MediaErrors.NotOwnedByUser(string.Join(", ", notOwned.Select(p => p.Id)));
        }

        // Check all are confirmed
        var unconfirmed = found.Where(p => !p.IsConfirmed).ToList();
        if (unconfirmed.Count > 0)
        {
            _logger.LogWarning("Media uploads not confirmed: {UnconfirmedIds}", string.Join(", ", unconfirmed.Select(p => p.Id)));
            return MediaErrors.NotConfirm;
        }

        // Check none are already linked
        var alreadyLinked = found.Where(p => p.IsLinked).ToList();
        if (alreadyLinked.Count > 0)
        {
            _logger.LogWarning("Media uploads already linked: {AlreadyLinkedIds}", string.Join(", ", alreadyLinked.Select(p => p.Id)));
            return MediaErrors.AlreadyLinked;
        }

        // Check context
        var invalidContext = found
            .Where(u => !_contextRegistry.IsItemContext(u.Context))
            .ToList();
        if (invalidContext.Count > 0)
        {
            _logger.LogWarning("Media uploads invalid context: {InvalidContext}", string.Join(", ", invalidContext.Select(p => p.Id)));
            return MediaErrors.WrongContext(invalidContext[0].Context, _contextRegistry.GetAllContext());
        }

        return null;
    }
}


