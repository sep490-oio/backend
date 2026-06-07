using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
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

        // An admin-terminated auction permanently blocks the seller from creating a NEW
        // auction for this item — only an administrator can bring it back via relist
        // (AdminRelistAuction creates the auction directly and bypasses this service).
        // The terminal-item-release handler returns the item to Active after a terminate,
        // so the item-status check above would otherwise let the seller re-create.
        if (existingAuctions.Any(x => string.Equals(x.Status.Id, AuctionStatus.Terminated.Id, StringComparison.Ordinal)))
            return AuctionErrors.Auction.TerminatedRequiresRelist;

        if (existingAuctions.Any(x => BlockingAuctionStatuses.Contains(x.Status.Id, StringComparer.Ordinal)))
            return AuctionErrors.Auction.ItemAlreadyHasAuction;

        var itemIdValue = item.Id.Value;
        var warehouseItemIds = await dbContext.Set<WarehouseItem>()
            .Where(wi => wi.ItemId == itemIdValue)
            .Select(wi => (Guid?)wi.Id.Value)
            .ToListAsync(cancellationToken);
            
        var auctionIds = await dbContext.Set<Auction>()
            .Where(a => a.ItemId == item.Id)
            .Select(a => (Guid?)a.Id.Value)
            .ToListAsync(cancellationToken);

        var auctionVogenIds = await dbContext.Set<Auction>()
            .Where(a => a.ItemId == item.Id)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        var orderIds = await dbContext.Set<Order>()
            .Where(o => auctionVogenIds.Contains(o.AuctionId))
            .Select(o => (Guid?)o.Id.Value)
            .ToListAsync(cancellationToken);

        var hasActiveDispute = await dbContext.Set<Dispute>()
            .AnyAsync(d =>
                d.Status.Id != "resolved" && d.Status.Id != "rejected" && d.Status.Id != "cancelled" && d.Status.Id != "closed" &&
                (
                    (d.WarehouseItemId != null && warehouseItemIds.Contains(d.WarehouseItemId)) ||
                    (d.CaseAuctionId != null && auctionIds.Contains(d.CaseAuctionId)) ||
                    (d.CaseOrderId != null && orderIds.Contains(d.CaseOrderId))
                ), cancellationToken);

        if (hasActiveDispute)
            return AuctionErrors.Auction.ItemHasActiveDispute;

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
