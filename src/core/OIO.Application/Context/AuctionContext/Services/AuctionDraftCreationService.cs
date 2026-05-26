using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Services;

internal sealed record AuctionDraftCreationRequest(
    decimal StartingPrice,
    decimal BidIncrement,
    decimal? ReservePrice,
    decimal? BuyNowPrice,
    string Currency,
    string AuctionType);

internal sealed class AuctionDraftCreationService(IDbContext dbContext)
{
    private static readonly string[] AllowedItemStatuses =
    [
        ItemStatus.Draft.Id,
        ItemStatus.PendingReview.Id,
        ItemStatus.PendingVerify.Id,
        ItemStatus.PendingConditionConfirmation.Id,
        ItemStatus.Approved.Id,
        ItemStatus.Active.Id
    ];

    private static readonly string[] BlockingAuctionStatuses =
    [
        AuctionStatus.Draft.Id,

        AuctionStatus.Approved.Id,
        AuctionStatus.Scheduled.Id,
        AuctionStatus.Active.Id,
        AuctionStatus.Ended.Id,
        AuctionStatus.Sold.Id,
        AuctionStatus.Completed.Id
    ];

    public async Task<Result<Auction, Error>> CreateAsync(
        Item item,
        UserId actorId,
        AuctionDraftCreationRequest request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (item.SellerId != actorId)
            return AuctionErrors.Auction.OnlyOwnerOfItem;

        if (item.Media.Count == 0)
            return AuctionErrors.Auction.ItemRequiresMedia;

        if (!AllowedItemStatuses.Contains(item.Status.Id, StringComparer.Ordinal))
            return AuctionErrors.Item.CannotAuction(item.Id, item.Status.Id);

        var existingAuctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(x => x.ItemId == item.Id)
            .ToListAsync(cancellationToken);

        if (existingAuctions.Any(x => string.Equals(x.Status.Id, AuctionStatus.PaymentDefaulted.Id, StringComparison.Ordinal)))
            return AuctionErrors.Auction.PaymentDefaultedRequiresRelist;

        if (existingAuctions.Any(x => BlockingAuctionStatuses.Contains(x.Status.Id, StringComparer.Ordinal)))
            return AuctionErrors.Auction.ItemAlreadyHasAuction;

        var currency = Currency.FromId(request.Currency);
        if (currency.HasNoValue)
            return Currency.Errors.NotSupported;

        var pricingResult = AuctionPricing.Create(
            startingPrice: request.StartingPrice,
            bidIncrement: request.BidIncrement,
            currency: currency.Value,
            reservePrice: request.ReservePrice,
            buyNowPrice: request.BuyNowPrice,
            isSealed: string.Equals(request.AuctionType, AuctionType.Sealed.Id, StringComparison.Ordinal));

        if (pricingResult.IsFailure)
            return pricingResult.Error;

        var auctionType = AuctionType.FromId(request.AuctionType);
        if (auctionType.HasNoValue)
            return AuctionErrors.Auction.InvalidAuctionType;

        return Auction.Create(
            sellerId: item.SellerId,
            itemId: item.Id,
            auctionType: auctionType.Value,
            pricing: pricingResult.Value,
            nowUtc: nowUtc);
    }
}
