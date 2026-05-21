using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.ValueObjects;

public sealed class AuctionInfo : ValueObject
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public QualificationWindow? Qualification { get; private set; }
    public bool AutoExtend { get; private set; }
    public int ExtensionMinutes { get; private set; }
    public int ExtensionCount { get; private set; }

    private AuctionInfo(){}

    private AuctionInfo(
        DateTime startTime,
        DateTime endTime,
        QualificationWindow? qualification,
        bool autoExtend,
        int extensionMinutes,
        int extensionCount = 0)
    {
        StartTime = startTime;
        EndTime = endTime;
        Qualification = qualification;
        AutoExtend = autoExtend;
        ExtensionMinutes = extensionMinutes;
        ExtensionCount = extensionCount;
    }

    public static Result<AuctionInfo, Error> Create(
        DateTime nowUtc,
        DateTime startTime,
        DateTime endTime,
        bool autoExtend = true,
        int extensionMinutes = 5,
        QualificationWindow? qualification = null)
    {
        if (qualification is null)
            return AuctionErrors.Auction.QualificationWindowRequired;

        var check = AuctionInfo
            .Check(isInvariant: true)
            .Field(startTime, x => x.StartTime)
            .NotInPast(() => nowUtc)
            .Field(endTime, x => x.EndTime)
            .NotInPast(() => nowUtc)
            .After(startTime)
            .Field(extensionMinutes, x => x.ExtensionMinutes)
            .NonNegative()
            .Field(qualification, x => x.Qualification);
        
        if(qualification is not null)
        {
            check.Field(qualification.StartTime, x => x.Qualification!.StartTime)
                .NotInPast(() => nowUtc)
                .Before(startTime);
            check.Field(qualification.EndTime, x => x.Qualification!.EndTime)
                .NotInPast(() => nowUtc)
                .Before(startTime)
                .After(qualification.StartTime);
        }

        var checkResult = check.ToUnitResult();

        if (checkResult.IsFailure)
        {
            return checkResult.Error;
        }

        return new AuctionInfo(
            startTime: startTime,
            endTime: endTime,
            qualification: qualification,
            autoExtend: autoExtend,
            extensionMinutes: extensionMinutes);
    }

    // ── Time queries ──

    public bool HasStarted(DateTime nowUtc) => nowUtc >= StartTime;
    public bool HasEnded(DateTime nowUtc) => nowUtc >= EndTime;
    public bool IsActive(DateTime nowUtc) => HasStarted(nowUtc) && !HasEnded(nowUtc);
    public TimeSpan RemainingTime(DateTime nowUtc) => HasEnded(nowUtc) ? TimeSpan.Zero : EndTime - nowUtc;
    public TimeSpan TotalDuration => EndTime - StartTime;

    // ── Qualification queries ──

    public bool HasQualification => Qualification is not null;
    public bool IsQualificationOpen(DateTime nowUtc) => Qualification?.IsOpen(nowUtc) ?? false;
    public bool IsQualificationClosed(DateTime nowUtc) => Qualification?.HasClosed(nowUtc) ?? true;

    // ── Auto-extend ──

    public bool ShouldExtend(DateTime nowUtc)
        => AutoExtend && IsActive(nowUtc) && RemainingTime(nowUtc).TotalMinutes <= ExtensionMinutes;

    public Result<AuctionInfo, Error> Extend(TimeSpan? maxDuration = null)
    {
        if (!AutoExtend)
            return Error.Validation("autoExtend", "AuctionInfo.AutoExtendDisabled",
                "Auto-extend is not enabled.");

        var newEnd = EndTime.AddMinutes(ExtensionMinutes);

        if (maxDuration.HasValue && (newEnd - StartTime) > maxDuration.Value)
            return Error.Validation("duration", "AuctionInfo.ExceedsMaxDuration",
                "Extension would exceed max allowed duration.");

        return new AuctionInfo(
            startTime: StartTime, 
            endTime: newEnd,
            qualification: Qualification,
            autoExtend: AutoExtend,
            extensionMinutes: ExtensionMinutes,
            extensionCount:  ExtensionCount + 1 );
    }

    /// <summary>
    /// Extends auction end time to compensate for a failed/expired buy-now reservation.
    /// During the reservation window, other participants were blocked from acting.
    /// Unlike <see cref="Extend"/>, this does not count toward <see cref="ExtensionCount"/>
    /// because it is compensatory, not a bid-triggered extension.
    /// </summary>
    public Result<AuctionInfo, Error> ExtendByCompensation(TimeSpan compensation)
    {
        if (compensation <= TimeSpan.Zero)
            return this;

        var newEndTime = EndTime.Add(compensation);

        return new AuctionInfo(
            startTime: StartTime,
            endTime: newEndTime,
            qualification: Qualification,
            autoExtend: AutoExtend,
            extensionMinutes: ExtensionMinutes,
            extensionCount: ExtensionCount);
    }

    /// <summary>
    /// Extends all auction timing (qualification window end, start time, end time) to
    /// compensate for a failed/expired buy-now reservation during the deposit phase.
    /// Maintains relative durations between phases.
    /// </summary>
    public Result<AuctionInfo, Error> ExtendAllByCompensation(TimeSpan compensation)
    {
        if (compensation <= TimeSpan.Zero)
            return this;

        var newQualification = Qualification?.ExtendBy(compensation);
        var newStartTime = StartTime.Add(compensation);
        var newEndTime = EndTime.Add(compensation);

        return new AuctionInfo(
            startTime: newStartTime,
            endTime: newEndTime,
            qualification: newQualification,
            autoExtend: AutoExtend,
            extensionMinutes: ExtensionMinutes,
            extensionCount: ExtensionCount);
    }

    public Result<AuctionInfo, Error> ForceStartQualification(DateTime nowUtc)
    {
        if (Qualification is null) return AuctionErrors.Auction.QualificationWindowRequired;
        if (nowUtc >= Qualification.StartTime) return this;

        var newQualStart = nowUtc;
        var newQualEnd = Qualification.EndTime;
        if (newQualEnd <= newQualStart) newQualEnd = newQualStart.AddMinutes(15);

        var newQualResult = QualificationWindow.Create(newQualStart, newQualEnd);
        if (newQualResult.IsFailure) return newQualResult.Error;

        var newStartTime = StartTime;
        if (newStartTime <= newQualEnd) newStartTime = newQualEnd.AddMinutes(1);

        return new AuctionInfo(
            startTime: newStartTime,
            endTime: EndTime,
            qualification: newQualResult.Value,
            autoExtend: AutoExtend,
            extensionMinutes: ExtensionMinutes,
            extensionCount: ExtensionCount);
    }

    public Result<AuctionInfo, Error> ForceStartBidding(DateTime nowUtc)
    {
        var newStartTime = nowUtc;
        var newEndTime = EndTime;
        if (newEndTime <= newStartTime) newEndTime = newStartTime.AddHours(1);

        QualificationWindow? newQual = Qualification;
        if (newQual is not null && newQual.EndTime >= newStartTime)
        {
            var qualStart = newQual.StartTime;
            if (qualStart >= newStartTime) qualStart = newStartTime.AddMinutes(-2);
            var qualEnd = newStartTime.AddMinutes(-1);
            
            var newQualResult = QualificationWindow.Create(qualStart, qualEnd);
            if (newQualResult.IsFailure) return newQualResult.Error;
            newQual = newQualResult.Value;
        }

        return new AuctionInfo(
            startTime: newStartTime,
            endTime: newEndTime,
            qualification: newQual,
            autoExtend: AutoExtend,
            extensionMinutes: ExtensionMinutes,
            extensionCount: ExtensionCount);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartTime;
        yield return EndTime;
        yield return AutoExtend;
        yield return ExtensionMinutes;
        if (Qualification is not null)
        {
            yield return Qualification.StartTime;
            yield return Qualification.EndTime;
        }
    }
}
