using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class AuctionPricing : ValueObject
{
    public decimal StartingAmount { get; private set; }
    public decimal? ReserveAmount { get; private set; }
    public decimal? BuyNowAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public decimal BidIncrementAmount { get; private set; }
    public Currency Currency { get; private set; }

   
    public Money NextMinimumBid => Money.Of(CurrentAmount + BidIncrementAmount, Currency);
    
    public Money StartingPrice => Money.Of(StartingAmount, Currency);
    public Money? ReservePrice => ReserveAmount.HasValue ? Money.Of(ReserveAmount.Value, Currency) : null;
    public Money? BuyNowPrice => BuyNowAmount.HasValue ? Money.Of(BuyNowAmount.Value, Currency) : null;
    public Money CurrentPrice => Money.Of(CurrentAmount, Currency);
    public Money BidIncrement => Money.Of(BidIncrementAmount, Currency);
    public bool HasReservePrice => ReserveAmount.HasValue;
    public bool HasBuyNowPrice => BuyNowAmount.HasValue;
    public bool ReserveMet => !HasReservePrice || CurrentAmount >= ReserveAmount!;
    public bool IsBuyNowAvailable =>
        HasBuyNowPrice && CurrentAmount < BuyNowAmount!.Value;

    private AuctionPricing() {} 
    
    private AuctionPricing(
        decimal startingPrice,
        decimal? reservePrice,
        decimal? buyNowPrice,
        decimal currentPrice,
        decimal bidIncrement,
        Currency currency)
    {
        StartingAmount = startingPrice;
        ReserveAmount = reservePrice;
        BuyNowAmount = buyNowPrice;
        CurrentAmount = currentPrice;
        BidIncrementAmount = bidIncrement;
        Currency = currency;
    }

    public static Result<AuctionPricing, Error> Create(
        decimal startingPrice,
        decimal bidIncrement,
        Currency currency,
        decimal? reservePrice = null,
        decimal? buyNowPrice = null)
    {
        var check = AuctionPricing.Check(isInvariant: true)
            .Field(bidIncrement, x => x.BidIncrementAmount)
            .Positive()
            .Field(reservePrice, x => x.ReserveAmount)
            .WhenHasValue(x => x.GreaterThanOrEqual(startingPrice))
            .Field(buyNowPrice, x => x.BuyNowAmount)
            .WhenHasValue(x => x.GreaterThan(startingPrice))
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AuctionPricing(
            startingPrice: startingPrice, 
            reservePrice: reservePrice,
            buyNowPrice: buyNowPrice,
            currentPrice: startingPrice,
            bidIncrement: bidIncrement,
            currency: currency);
    }

    public Result<AuctionPricing, Error> WithNewBid(decimal bidAmount, decimal minimumRequired)
    {
        var check = AuctionPricing.Check(isInvariant: true)
            .Field(bidAmount, "BidAmount")
            .GreaterThanOrEqual(minimumRequired)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AuctionPricing(
            startingPrice: StartingAmount, 
            reservePrice: ReserveAmount,
            buyNowPrice: BuyNowAmount,
            currentPrice: bidAmount,
            bidIncrement: BidIncrementAmount,
            currency: Currency);
    }

    public Result<AuctionPricing, Error> WithBuyNow()
    {
        if (!IsBuyNowAvailable)
            return Error.Validation("buyNow", "AuctionPricing.BuyNowNotAvailable",
                "Buy-now is not available.");

        return new AuctionPricing(
            startingPrice: StartingAmount, 
            reservePrice: ReserveAmount,
            buyNowPrice: BuyNowAmount,
            currentPrice: BuyNowAmount!.Value,
            bidIncrement: BidIncrementAmount,
            currency: Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartingAmount;
        yield return ReserveAmount ?? -1m;
        yield return BuyNowAmount ?? -1m;
        yield return CurrentAmount;
        yield return BidIncrementAmount;
        yield return Currency;
    }
}
