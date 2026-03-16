using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Commands.CreateItem;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.AuctionContext.Services;
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
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Commands.CreateAuction;

public sealed record CreateAuctionCommand(
    // Item info
    string Title,
    string Condition,
    Guid? CategoryId = null,
    string? Description = null,
    int Quantity = 1,
    string? Attributes = null,
    IReadOnlyList<MediaAttachment>? Media = null,
    // Auction pricing
    decimal StartingPrice = 0,
    decimal BidIncrement = 0,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    int ExtensionMinutes = 5,
    string Currency = "VND",
    string AuctionType = "regular") : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        var check = CreateAuctionCommand.Check()
            .WithOwnerName("CreateAuction")
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
            .Field(StartingPrice)
            .NonNegative()
            .Field(BidIncrement)
            .NonNegative()
            .Field(ReservePrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(BuyNowPrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(ExtensionMinutes)
            .BetweenInclusive(1, 30)
            .Field(Currency)
            .NotWhiteSpace()
            .ExactLength(3)
            .Field(AuctionType)
            .NotWhiteSpace()
            .InSet(Domain.Context.AuctionContext.Enums.AuctionType.All.Select(x => x.Id))
            .ToViolationsError();

        if (Media is null)
            return check;

        for (var i = 0; i < Media.Count; i++)
        {
            check.Add(Media[i].Validate());
        }

        return check;
    }
}

internal sealed class CreateAuctionCommandHandler
    : ICommandHandler<CreateAuctionCommand, AuctionDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;
    private readonly UploadContextRegistry _contextRegistry;
    private readonly IMediaRelocationService _mediaRelocationService;
    private readonly AuctionDraftCreationService _auctionDraftCreationService;
    private readonly ILogger<CreateAuctionCommandHandler> _logger;

    public CreateAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        IClock clock,
        UploadContextRegistry contextRegistry,
        IMediaRelocationService mediaRelocationService,
        AuctionDraftCreationService auctionDraftCreationService,
        ILogger<CreateAuctionCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _appConfigs = appConfigs;
        _clock = clock;
        _contextRegistry = contextRegistry;
        _mediaRelocationService = mediaRelocationService;
        _auctionDraftCreationService = auctionDraftCreationService;
        _logger = logger;
    }

    public async Task<Result<AuctionDto, Error>> Handle(
        CreateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var sellerId = _currentUser.UserId;

        // ── Validate category ──
        CategoryId? categoryId = null;
        if (request.CategoryId.HasValue)
        {
            categoryId = CategoryId.From(request.CategoryId.Value);
            var categoryExists = await _dbContext.Set<Category>()
                .AnyAsync(x => x.Id == categoryId, cancellationToken);
            if (!categoryExists)
                return AuctionErrors.Category.NotFound(categoryId.Value);
        }

        // ── Load & validate media uploads ──
        List<MediaUpload>? mediaUploads = null;
        if (request.Media is { Count: > 0 })
        {
            var pendingIds = request.Media.Select(i => MediaUploadId.From(i.MediaUploadId)).ToList();
            mediaUploads = await _dbContext.Set<MediaUpload>()
                .Where(p => pendingIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var validationError = ValidateMediaUploads(
                mediaUploads, pendingIds, sellerId);
            if (validationError is not null)
                return validationError;
        }

        // ── Create Item ──
        var (_, isFailure, title, error) = ItemTitle.Create(request.Title);
        if (isFailure) return error;

        var condition = ItemCondition.FromId(request.Condition);

        var item = Item.Create(
            sellerId: sellerId,
            title: title,
            condition: condition.Value,
            nowUtc: nowUtc,
            categoryId: categoryId,
            description: request.Description,
            quantity: request.Quantity,
            attributes: request.Attributes);

        if (mediaUploads is not null && request.Media is not null)
        {
            foreach (var mediaReq in request.Media.OrderBy(i => i.SortOrder))
            {
                var mediaUploadId = MediaUploadId.From(mediaReq.MediaUploadId);
                var upload = mediaUploads.First(p => p.Id == mediaUploadId);
                var maxForType = await _contextRegistry.GetMaxForEntityMediaAsync(
                    "item", upload.ResourceType, cancellationToken);
                item.AddMedia(nowUtc, upload, mediaReq.IsPrimary, maxForType, mediaReq.SortOrder);
            }
        }


        // ── Create Auction (pricing only, no timing) ──
        var auctionResult = await _auctionDraftCreationService.CreateAsync(
            item,
            sellerId,
            new AuctionDraftCreationRequest(
                request.StartingPrice,
                request.BidIncrement,
                request.ReservePrice,
                request.BuyNowPrice,
                request.Currency,
                request.AuctionType),
            nowUtc,
            cancellationToken);
        if (auctionResult.IsFailure) return auctionResult.Error;

        var auction = auctionResult.Value;

        _dbContext.Insert(item);
        _dbContext.Insert(auction);

        // ── Submit immediately if requested ──
        if (mediaUploads is not null)
        {
            foreach (var upload in mediaUploads)
                await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auction.ToDto(nowUtc,
            await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken));
    }

    private Error? ValidateMediaUploads(
        List<MediaUpload> found,
        List<MediaUploadId> requestedIds,
        Domain.Context.UserContext.ValueObjects.Ids.UserId userId)
    {
        var foundIds = found.Select(p => p.Id).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            _logger.LogWarning("Media uploads not found: {MissingIds}", string.Join(", ", missingIds));
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        var notOwned = found.Where(p => p.UserId != userId).ToList();
        if (notOwned.Count > 0)
        {
            _logger.LogWarning("Media uploads not owned by user {UserId}: {Ids}", userId,
                string.Join(", ", notOwned.Select(p => p.Id)));
            return MediaErrors.NotOwnedByUser(string.Join(", ", notOwned.Select(p => p.Id)));
        }

        var unconfirmed = found.Where(p => !p.IsConfirmed).ToList();
        if (unconfirmed.Count > 0)
        {
            _logger.LogWarning("Media uploads not confirmed: {Ids}", string.Join(", ", unconfirmed.Select(p => p.Id)));
            return MediaErrors.NotConfirm;
        }

        var alreadyLinked = found.Where(p => p.IsLinked).ToList();
        if (alreadyLinked.Count > 0)
        {
            _logger.LogWarning("Media uploads already linked: {Ids}", string.Join(", ", alreadyLinked.Select(p => p.Id)));
            return MediaErrors.AlreadyLinked;
        }

        return null;
    }
}
