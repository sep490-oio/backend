using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.Shared.ValueObjects;

public sealed class Rating : ValueObject
{
    public const short MinValue = 1;
    public const short MaxValue = 5;

    public short Value { get; }

    public Rating()
    {
        
    }

    private Rating(short value) => Value = value;

    public static Result<Rating, Error> Create(short value)
    {
        var result = Rating.Check(isInvariant: true)
            .Field(value)
            .BetweenInclusive(MinValue, MaxValue)
            .ToUnitResult();

        if (result.IsFailure)
            return result.Error;
        
        return new Rating(value);
    }

    public static implicit operator short(Rating r) => r.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}