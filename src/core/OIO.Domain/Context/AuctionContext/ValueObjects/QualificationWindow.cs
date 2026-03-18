using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class QualificationWindow : ValueObject
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    
    private QualificationWindow(){}

    private QualificationWindow(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }

    public static Result<QualificationWindow, Error> Create(DateTime startTime, DateTime endTime)
    {
        var check = QualificationWindow
            .Check(isInvariant: true)
            .Field(startTime, x => x.StartTime)
            .Field(endTime, x => x.EndTime)
            .After(startTime)
            .ToUnitResult();
        
        if (check.IsFailure)
            return check.Error;
        
        return new QualificationWindow(startTime, endTime);
    }

    public bool IsOpen(DateTime nowUtc) => nowUtc >= StartTime && nowUtc < EndTime;
    public bool HasClosed(DateTime nowUtc) => nowUtc >= EndTime;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
    }
}