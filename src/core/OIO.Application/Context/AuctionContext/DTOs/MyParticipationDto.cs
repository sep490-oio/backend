namespace OIO.Application.Context.AuctionContext.DTOs;

/// <summary>
/// Unified view of a user's participation in an auction — covers both
/// deposit-only and deposit+bid scenarios.
/// </summary>
public sealed record MyParticipationDto(
    Guid AuctionId,
    Guid ItemId,
    string ItemTitle,
    string? PrimaryImageUrl,
    string AuctionStatus,
    MoneyDto CurrentPrice,
    // Deposit info — always present (every participant deposited)
    decimal DepositAmount,
    string DepositCurrency,
    string DepositStatus,
    DateTime DepositedAt,
    // Bid info — null when user deposited but never bid
    MoneyDto? MyLatestBidAmount,
    string? BidPosition,
    DateTime? LastBidAt,
    int BidCountForUser,
    // Buyer-direct-pay enrichment (same as MyBidDto)
    Guid? OrderId = null,
    string? OrderStatus = null,
    bool CanPayNow = false);
