using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

/// <summary>
/// Immutable record of a price change during an auction.
/// Maintains an audit trail of all price updates.
/// </summary>
public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>
{
    public AuctionId AuctionId { get; private set; }
    public Money Price { get; private set; }
    public BidId? BidId { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private AuctionPriceHistory() { }

    private AuctionPriceHistory(
        AuctionPriceHistoryId id,
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId? bidId = null)
        : base(id)
    {
        AuctionId = auctionId;
        Price = price;
        RecordedAt = recordedAt;
        BidId = bidId;
    }

    /// <summary>
    /// Creates a new price history record with validation.
    /// </summary>
    public static Result<AuctionPriceHistory, Error> Create(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId? bidId = null)
    {
        if (auctionId.Value == Guid.Empty)
            return AuctionErrors.PriceHistory.AuctionIdEmpty;

        if (price.Amount < 0)
            return AuctionErrors.PriceHistory.NegativePrice;

        if (recordedAt == default)
            return AuctionErrors.PriceHistory.InvalidRecordedAt;

        if (recordedAt.Kind != DateTimeKind.Utc)
            return AuctionErrors.PriceHistory.NonUtcDateTime;

        var priceHistory = new AuctionPriceHistory(
            AuctionPriceHistoryId.From(Guid.CreateVersion7()),
            auctionId,
            price,
            recordedAt,
            bidId);

        return Result.Success<AuctionPriceHistory, Error>(priceHistory);
    }
}