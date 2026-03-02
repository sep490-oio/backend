using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class WinningConditions : ValueObject
{
    public Money StartingPrice { get; }
    public Money? ReservePrice { get; }
    public Money? BuyNowPrice { get; }

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
            .NonNegative()
            .PrecisionScale(18, 2)
            .Field(reservePrice?.Amount, "ReservePrice")
            .WhenHasValue(f => f.GreaterThanOrEqual(startingPrice.Amount, "Giá sàn không được thấp hơn giá khởi điểm"))
            .Field(buyNowPrice?.Amount, "BuyNowPrice")
            .WhenHasValue(f => f.GreaterThan(startingPrice.Amount, "Giá mua ngay phải cao hơn giá khởi điểm"))
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