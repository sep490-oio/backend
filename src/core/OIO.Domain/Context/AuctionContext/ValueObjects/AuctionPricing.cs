using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class AuctionPricing : ValueObject
{
    private decimal _startingPrice;
    private readonly decimal? _reservePrice;
    private readonly decimal? _buyNowPrice;
    private readonly decimal _currentPrice;
    private readonly decimal _bidIncrement;
    private readonly string _currency;

    // ── Public Money API (domain logic dùng these) ──
    public Money StartingPrice => Money.Of(_startingPrice, new Currency(_currency));
    public Money? ReservePrice => _reservePrice.HasValue ? Money.Of(_reservePrice.Value, new Currency(_currency)) : null;
    public Money? BuyNowPrice => _buyNowPrice.HasValue ? Money.Of(_buyNowPrice.Value, new Currency(_currency)) : null;
    public Money CurrentPrice => Money.Of(_currentPrice, new Currency(_currency));
    public Money BidIncrement => Money.Of(_bidIncrement, new Currency(_currency));
    public Money NextMinimumBid => Money.Of(_currentPrice + _bidIncrement, new Currency(_currency));
    public Currency Currency => new Currency(_currency);
    
    public bool HasReservePrice => _reservePrice.HasValue;
    public bool HasBuyNowPrice => _buyNowPrice.HasValue;
    public bool ReserveMet => !HasReservePrice || _currentPrice >= _reservePrice!;
    public bool IsBuyNowAvailable =>
        HasBuyNowPrice && _currentPrice < _buyNowPrice!.Value;

    private AuctionPricing() {} 
    
    private AuctionPricing(
        decimal startingPrice,
        decimal? reservePrice,
        decimal? buyNowPrice,
        decimal currentPrice,
        decimal bidIncrement,
        string currency)
    {
        _startingPrice = startingPrice;
        _reservePrice = reservePrice;
        _buyNowPrice = buyNowPrice;
        _currentPrice = currentPrice;
        _bidIncrement = bidIncrement;
        _currency = currency;
    }

    public static Result<AuctionPricing, Error> Create(
        decimal startingPrice,
        decimal bidIncrement,
        Currency currency,
        decimal? reservePrice = null,
        decimal? buyNowPrice = null)
    {
        var check = AuctionPricing.Check(isInvariant: true)
            .Field(bidIncrement, x => x.BidIncrement)
            .Positive()
            .Field(reservePrice, x => x.ReservePrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(startingPrice))
            .Field(buyNowPrice, x => x.BuyNowPrice)
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
            currency: currency.Id);
    }

    public Result<AuctionPricing, Error> WithNewBid(decimal bidAmount)
    {
        var check = AuctionPricing.Check(isInvariant: true)
            .Field(bidAmount, "BidAmount")
            .GreaterThanOrEqual(NextMinimumBid.Amount)
            .ToUnitResult();
        
        if (check.IsFailure)
        {
            return check.Error;
        }

        return new AuctionPricing(
            startingPrice: _startingPrice, 
            reservePrice: _reservePrice,
            buyNowPrice: _buyNowPrice,
            currentPrice: bidAmount,
            bidIncrement: _bidIncrement,
            currency: Currency.Id);
    }

    public Result<AuctionPricing, Error> WithBuyNow()
    {
        if (!IsBuyNowAvailable)
            return Error.Validation("buyNow", "AuctionPricing.BuyNowNotAvailable",
                "Buy-now is not available.");

        return new AuctionPricing(
            startingPrice: _startingPrice, 
            reservePrice: _reservePrice,
            buyNowPrice: _buyNowPrice,
            currentPrice: _buyNowPrice!.Value,
            bidIncrement: _bidIncrement,
            currency: Currency.Id);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return _startingPrice;
        yield return _reservePrice ?? -1m;
        yield return _buyNowPrice ?? -1m;
        yield return _currentPrice;
        yield return _bidIncrement;
        yield return _currency;
    }
}