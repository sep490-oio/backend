using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class BidIncrement : ValueObject
{
    public Money Value { get; }
    private BidIncrement() { }
    private BidIncrement(Money value) => Value = value;

    public static Result<BidIncrement, Error> Create(Money value)
    {
        var result = BidIncrement.Check(isInvariant: true)
            .Field(value.Amount, "BidIncrement")!
            .NonZero()
            .Positive("Bid increment must be a positive value.",AuctionErrors.Auction.InvalidIncrement)
            .PrecisionScale(18, 2)
            .ToResult();

        if (result.IsFailure) return result.Error;

        return new BidIncrement(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Value];
}