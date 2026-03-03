using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class WinningConditions : ValueObject
{
    public Money StartingPrice { get; }
    public Money? ReservePrice { get; }
    public Money? BuyNowPrice { get; }
    private WinningConditions() { }

    private WinningConditions(Money startingPrice, Money? reservePrice, Money? buyNowPrice)
    {
        StartingPrice = startingPrice;
        ReservePrice = reservePrice;
        BuyNowPrice = buyNowPrice;
    }

    public static Result<WinningConditions, Error> Create(
        Money startingPrice,
        Money? reservePrice = null,
        Money? buyNowPrice = null)
    {
        var result = WinningConditions.Check(isInvariant: true)
            .Field(startingPrice.Amount, "StartingPrice")!
            .NonNegative("Starting price must be a non-negative value.",AuctionErrors.Auction.InvalidStartingPrice) 
            .PrecisionScale(18, 2)
            .Field(reservePrice?.Amount, "ReservePrice")
            .WhenHasValue(f => f.GreaterThanOrEqual(startingPrice.Amount, "Reserve price must be greater than or equal to starting price.",AuctionErrors.Auction.InvalidReserve))
            .Field(buyNowPrice?.Amount, "BuyNowPrice")
            .WhenHasValue(f => f.GreaterThan(startingPrice.Amount, "Buy now price must be greater than starting price.",AuctionErrors.Auction.InvalidBuyNow))
            .ToResult();

        if (result.IsFailure) return result.Error;

        return new WinningConditions(startingPrice, reservePrice, buyNowPrice);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartingPrice;
        yield return ReservePrice;
        yield return BuyNowPrice;
    }
}