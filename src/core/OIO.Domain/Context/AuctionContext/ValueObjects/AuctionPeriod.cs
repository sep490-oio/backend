using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class AuctionPeriod : ValueObject
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    private AuctionPeriod() { } // ← add this for EF
    private AuctionPeriod(DateTime start, DateTime end)
    {
        StartTime = start;
        EndTime = end;
    }

    public static Result<AuctionPeriod, Error> Create(DateTime start, DateTime end)
    {
        var result = AuctionPeriod.Check(isInvariant: true)
            .Field(start, x => x.StartTime)!.Utc()
            .Field(end, x => x.EndTime)!.Utc().After(start, "End time must be later than start time.",AuctionErrors.Auction.InvalidPeriod)
            .ToResult();

        if (result.IsFailure) return result.Error;

        return new AuctionPeriod(start, end);
    }

    public bool IsActive(DateTime nowUtc) => nowUtc >= StartTime && nowUtc <= EndTime;
    public bool HasEnded(DateTime nowUtc) => nowUtc > EndTime;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
    }
}