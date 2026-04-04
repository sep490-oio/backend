using CSharpFunctionalExtensions;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class Report : BaseEntity<ReportId>, ICreatedAtEntity
{
    public UserId ReporterId { get; private set; }
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string ReasonCode { get; private set; }
    public string? Description { get; private set; }
    public string? Attachments { get; private set; }
    public ReportStatus Status { get; private set; }
    public UserId? AssignedTo { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? EscalatedEmergencyAt { get; private set; }
    public DisputeId? DisputeId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private Report() { }

    public static Report Create(
        UserId reporterId,
        string entityType,
        Guid entityId,
        string reasonCode,
        string? description,
        string? attachments,
        DateTime nowUtc)
    {
        return new Report
        {
            Id = ReportId.From(Guid.CreateVersion7()),
            ReporterId = reporterId,
            EntityType = entityType,
            EntityId = entityId,
            ReasonCode = reasonCode,
            Description = description,
            Attachments = attachments,
            Status = ReportStatus.Open,
            CreatedAt = nowUtc,
            ModifiedAt = nowUtc
        };
    }

    public void Assign(UserId adminId, DateTime nowUtc)
    {
        AssignedTo = adminId;
        AssignedAt = nowUtc;
        Status = ReportStatus.UnderReview;
        ModifiedAt = nowUtc;
    }

    public UnitResult<Error> Resolve(string? resolutionNotes, bool dismissed, DateTime nowUtc)
    {
        if (Status == ReportStatus.ActionTaken || Status == ReportStatus.Dismissed || Status == ReportStatus.Closed)
            return UnitResult.Failure(Error.Conflict("Report.AlreadyResolved",
                "Cannot resolve a report that is already resolved, dismissed, or closed."));

        ResolutionNotes = resolutionNotes;
        ResolvedAt = nowUtc;
        Status = dismissed ? ReportStatus.Dismissed : ReportStatus.ActionTaken;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public void Close(DateTime nowUtc)
    {
        Status = ReportStatus.Closed;
        ModifiedAt = nowUtc;
    }

    public void MarkEscalated(DateTime nowUtc)
    {
        EscalatedEmergencyAt = nowUtc;
        Status = ReportStatus.ActionTaken;
        ModifiedAt = nowUtc;
    }

    public UnitResult<Error> EscalateToDispute(DisputeId disputeId, DateTime nowUtc)
    {
        if (Status == ReportStatus.Dismissed || Status == ReportStatus.Closed)
            return UnitResult.Failure(Error.Conflict("Report.CannotEscalate", "Cannot escalate a dismissed or closed report."));
        DisputeId = disputeId;
        Status = ReportStatus.ActionTaken;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }
}
