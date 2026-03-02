using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class BidIncrement : ValueObject
{
    public Money Value { get; }

    private BidIncrement(Money value) => Value = value;

    public static Result<BidIncrement, Error> Create(Money value)
    {
        var result = BidIncrement.Check(isInvariant: true)
            .Field(value.Amount, "BidIncrement")!
            .Positive("Bước nhảy giá phải lớn hơn 0")
            .PrecisionScale(18, 2)
            .ToResult();

        if (result.IsFailure) return result.Error;

        return new BidIncrement(value);
    }

    protected override IEnumerable<object?> GetEqualityComponents() => [Value];
}