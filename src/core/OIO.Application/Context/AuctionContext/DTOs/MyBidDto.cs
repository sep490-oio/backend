namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record MyBidDto(
    Guid AuctionId,
    Guid ItemId,
    string ItemTitle,
    string? PrimaryImageUrl,
    string AuctionStatus,
    MoneyDto CurrentPrice,
    MoneyDto MyLatestBidAmount,
    string Position,
    DateTime? WonAt,
    DateTime LastBidAt,
    int BidCountForUser,
    // Buyer-direct-pay enrichment: when the winning bidder has an order
    // waiting to be paid, the FE routes "Pay Now" straight to
    // /checkout/{orderId} instead of scanning /me/orders.
    Guid? OrderId = null,
    string? OrderStatus = null,
    bool CanPayNow = false);