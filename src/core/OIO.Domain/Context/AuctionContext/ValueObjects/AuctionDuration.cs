using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class AuctionDuration : ValueObject
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    
    public TimeSpan Duration => EndTime - StartTime;

    private AuctionDuration(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
    
    // Factory method to create an AuctionDuration with validation
    //MinDuration and MaxDuration get from app settings
    public static Result<AuctionDuration, Error> Create(
        DateTime startTime,
        DateTime endTime,
        TimeSpan minDuration,
        TimeSpan maxDuration,
        DateTime nowUtc)
    {
        var check = AuctionDuration
            .Check(isInvariant: true)
            .Field(startTime, x => x.StartTime)
            .NotInPast(() => nowUtc)
            .Field(endTime, x => x.EndTime)
            .NotInPast(() => nowUtc)
            .After(startTime);
        var result = check.ToResult();

        if (result.IsFailure)
        {
            return result.Error;
        }

        var duration = endTime - startTime;

        check.Field(duration, x => x.Duration)
            .TimeRange(minDuration, maxDuration);
        
        result = check.ToResult();
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        return new AuctionDuration(startTime, endTime);
    }

    public bool IsActive(DateTime now) => now >= StartTime && now < EndTime;
    
    public bool HasStarted(DateTime now) => now >= StartTime;
    public bool HasEnded(DateTime now) => now >= EndTime;
    
    public TimeSpan RemainingTime(DateTime now) =>
        HasEnded(now) ? TimeSpan.Zero : EndTime - now;

    public Result<AuctionDuration, Error> Extend(
        TimeSpan extension,
        TimeSpan maxDuration)
    {
        var newEndTime = EndTime.Add(extension);
        var newDuration = newEndTime - StartTime;

        var result = AuctionDuration
            .Check(isInvariant: true)
            .Field(newDuration, x => x.Duration)
            .Before(maxDuration)
            .ToResult();

        if (result.IsFailure)
        {
            return result.Error;
        }

        return new AuctionDuration(StartTime, newEndTime);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
    }
}