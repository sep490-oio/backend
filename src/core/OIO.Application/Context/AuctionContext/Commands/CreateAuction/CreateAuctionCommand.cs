using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.CreateAuction;

public sealed record CreateAuctionCommand(
    DateTime NowUtc,
    Guid ItemId,
    decimal StartingPrice,
    decimal BidIncrement,
    DateTime StartTime,
    DateTime EndTime,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    bool AutoExtend = true,
    int ExtensionMinutes = 5,
    string Currency = "VND") : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateAuctionCommand.Check()
            .WithOwnerName("CreateAuction")
            .Field(ItemId)
            .NotEmptyGuid()
            .Field(StartingPrice)
            .NonNegative()
            .Field(BidIncrement)
            .NonNegative()
            .Field(StartTime)
            .NotInPast(() => NowUtc)
            .Field(EndTime)
            .GreaterThan(StartTime)
            .Field(ReservePrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(BuyNowPrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(ExtensionMinutes)
            .BetweenInclusive(1, 30)
            .Field(Currency)
            .NotWhiteSpace()
            .ExactLength(3);
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

    public CreateAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _appConfigs = appConfigs;
        _clock = clock;
    }

    public async Task<Result<AuctionDto, Error>> Handle(
        CreateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        
        var currency = Currency.FromId(request.Currency).Value;
        var (_, isFailure, auctionPricing, error) = AuctionPricing.Create(
            startingPrice: request.StartingPrice,
            bidIncrement: request.BidIncrement,
            currency: currency,
            reservePrice: request.ReservePrice,
            buyNowPrice: request.BuyNowPrice);
        
        if (isFailure)
        {
            return error;
        }
        
        
        (_, isFailure, var auctionInfo, error) = AuctionInfo.Create(
            nowUtc: nowUtc,
            startTime: request.StartTime,
            endTime: request.EndTime,
            autoExtend: request.AutoExtend,
            extensionMinutes: request.ExtensionMinutes);
        
        if (isFailure)
        {
            return error;
        }
        
        var itemId = ItemId.From(request.ItemId);
        var sellerId = _currentUser.UserId;
        
        var item  = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId, 
            queryBuilder: query => query
                .Include(i => i.Media.OrderBy(img => img.SortOrder)) ,
            cancellationToken: cancellationToken);

        if (item is null)
        {
            return AuctionErrors.Item.NotFound(itemId);
        }

        if (item.SellerId != sellerId)
        {
            return AuctionErrors.Auction.OnlyOwnerOfItem;
        }

        if (!item.IsAvailableForAuction)
        {
            return AuctionErrors.Item.CannotAuction( itemId, item.Status.Id);
        }
        
        var auctionWithSameItem = await _dbContext.Set<Auction>()
            .AnyAsync(a => a.ItemId == itemId &&
                           (a.Status == AuctionStatus.Draft ||
                            a.Status == AuctionStatus.Pending ||
                            a.Status == AuctionStatus.Active), cancellationToken);

        if (auctionWithSameItem)
        {
            return AuctionErrors.Auction.ItemAlreadyInAuction;
        }
        
        (_, isFailure,var auction, error) = Auction.Create(
            sellerId: sellerId,
            itemId: itemId, 
            pricing: auctionPricing,
            info: auctionInfo,
            nowUtc: nowUtc);

        if(isFailure)
        {
            return error;
        }
        
        item.MarkInAuction(nowUtc);
        
        _dbContext.Insert(auction);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return auction.ToDto(nowUtc, await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken));
        
    }
}