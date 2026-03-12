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
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.AddMediaToItem;

public sealed record AddMediaToItemCommand(
    Guid ItemId,
    Guid MediaUploadId,
    bool IsPrimary = false,
    int? SortOrder = null) : ICommand<ItemMediaDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddMediaToItemCommand.Check()
            .WithOwnerName("AddMediaToItem")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(MediaUploadId)
            .NotEmptyGuid();
    }
}

internal sealed class AddMediaToItemCommandHandler
    : ICommandHandler<AddMediaToItemCommand, ItemMediaDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly ILogger<AddMediaToItemCommandHandler> _logger;

    public AddMediaToItemCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock,
        UploadContextRegistry contextRegistry,
        ILogger<AddMediaToItemCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _logger = logger;
    }

    public async Task<Result<ItemMediaDto, Error>> Handle(
        AddMediaToItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .Include(x => x.Media),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        if (item.SellerId != _currentUser.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to add media to item {ItemId} which they do not own.", _currentUser.UserId, itemId);
            return AuctionErrors.Item.NotOwnedByUser(itemId, _currentUser.UserId);
        }
        
        var nowUtc = _clock.UtcNow;
        
        var mediaUploadId = MediaUploadId.From(request.MediaUploadId);
        var upload = await _dbContext.GetByIdAsync<MediaUpload, MediaUploadId>(
            id: mediaUploadId,
            cancellationToken: cancellationToken
        );
            
        if (upload is null)
            return MediaErrors.NotFound(mediaUploadId);

        if (upload.UserId != _currentUser.UserId)
        {
            _logger.LogWarning("User {UserId} attempted to use media upload {MediaUploadId} which they do not own.", _currentUser.UserId, mediaUploadId);
            return MediaErrors.NotOwnedByUser(upload.Id);
        }

        if (!upload.IsConfirmed)
            return MediaErrors.NotConfirm;

        if (upload.IsLinked)
            return MediaErrors.AlreadyLinked;

        // Validate context is for items
        if (!_contextRegistry.IsItemContext(upload.Context))
        {
            _logger.LogWarning("Media upload {MediaUploadId} has invalid context {Context} for item {ItemId}.", mediaUploadId, upload.Context, itemId);
            return MediaErrors.WrongContext(upload.Context, await _contextRegistry.GetAllContextAsync());
        }
        
        // Get max limit for this resource type from config
        var maxForType = await _contextRegistry.GetMaxForEntityMediaAsync("item", upload.ResourceType);


        var (_, isFailure, image, error) = item.AddMedia(
            nowUtc,
            upload,
            request.IsPrimary,
            maxForType,
            request.SortOrder
            );

        if (isFailure)
        {
            return error;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return image.ToDto();
        
    }
}