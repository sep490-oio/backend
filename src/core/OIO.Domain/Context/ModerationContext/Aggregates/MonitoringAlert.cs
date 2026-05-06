using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ModerationContext.Aggregates;

public sealed class MonitoringAlert : BaseEntity<MonitoringAlertId>, ICreatedAtEntity
{
    public string EntityType { get; private set; }
    public Guid EntityId { get; private set; }
    public string AlertType { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string Payload { get; private set; }
    public AlertStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public Guid? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAt { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public DateTime? SlaDueAt { get; private set; }
    public string? ResolutionOutcome { get; private set; }
    public string? ResolutionReason { get; private set; }
    public string Fingerprint { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private MonitoringAlert() { }

    public static MonitoringAlert Create(
        string entityType,
        Guid entityId,
        string alertType,
        AlertSeverity severity,
        string payload,
        DateTime nowUtc)
    {
        var normalizedPayload = string.IsNullOrWhiteSpace(payload) ? "{}" : payload.Trim();

        return new MonitoringAlert
        {
            Id = MonitoringAlertId.From(Guid.CreateVersion7()),
            EntityType = entityType.Trim(),
            EntityId = entityId,
            AlertType = alertType.Trim(),
            Severity = severity,
            Payload = normalizedPayload,
            Status = AlertStatus.Open,
            SlaDueAt = CalculateSlaDueAt(severity, nowUtc),
            Fingerprint = BuildFingerprint(entityType, entityId, alertType, normalizedPayload),
            CreatedAt = nowUtc
        };
    }

    public UnitResult<Error> Assign(Guid adminId, DateTime nowUtc)
    {
        if (IsTerminal())
            return UnitResult.Failure(TerminalStateError("assign"));

        AssignedTo = adminId;
        AssignedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Unassign()
    {
        if (IsTerminal())
            return UnitResult.Failure(TerminalStateError("unassign"));

        AssignedTo = null;
        AssignedAt = null;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Acknowledge(Guid adminId, string? notes, DateTime nowUtc)
    {
        if (Status != AlertStatus.Open)
            return UnitResult.Failure(InvalidTransitionError(Status.Id, AlertStatus.Acknowledged.Id));

        Status = AlertStatus.Acknowledged;
        AcknowledgedBy = adminId;
        AcknowledgedAt = nowUtc;
        Notes = MergeNotes(Notes, notes);
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resolve(Guid adminId, string outcome, string reason, bool ignored, DateTime nowUtc)
    {
        if (Status != AlertStatus.Open && Status != AlertStatus.Acknowledged)
            return UnitResult.Failure(InvalidTransitionError(Status.Id, ignored ? AlertStatus.Ignored.Id : AlertStatus.Resolved.Id));

        if (string.IsNullOrWhiteSpace(outcome))
            return UnitResult.Failure<Error>(Error.Validation("resolutionOutcome", "MonitoringAlert.ResolutionOutcomeRequired", "Resolution outcome is required."));

        if (string.IsNullOrWhiteSpace(reason))
            return UnitResult.Failure<Error>(Error.Validation("resolutionReason", "MonitoringAlert.ResolutionReasonRequired", "Resolution reason is required."));

        Status = ignored ? AlertStatus.Ignored : AlertStatus.Resolved;
        ResolvedBy = adminId;
        ResolvedAt = nowUtc;
        ResolutionOutcome = outcome.Trim();
        ResolutionReason = reason.Trim();
        Notes = MergeNotes(Notes, reason);
        return UnitResult.Success<Error>();
    }

    private bool IsTerminal() => Status == AlertStatus.Resolved || Status == AlertStatus.Ignored;

    private static Error InvalidTransitionError(string from, string to) =>
        Error.Conflict(
            "MonitoringAlert.InvalidTransition",
            $"Cannot transition monitoring alert from '{from}' to '{to}'.");

    private Error TerminalStateError(string action) =>
        Error.Conflict(
            "MonitoringAlert.AlreadyTerminal",
            $"Cannot {action} monitoring alert when status is '{Status.Id}'.");

    private static DateTime CalculateSlaDueAt(AlertSeverity severity, DateTime nowUtc)
    {
        if (severity == AlertSeverity.Critical) return nowUtc.AddHours(4);
        if (severity == AlertSeverity.High) return nowUtc.AddDays(1);
        if (severity == AlertSeverity.Medium) return nowUtc.AddDays(3);
        return nowUtc.AddDays(7);
    }

    private static string BuildFingerprint(string entityType, Guid entityId, string alertType, string payload)
    {
        var normalized = $"{entityType.Trim().ToLowerInvariant()}:{entityId:N}:{alertType.Trim().ToLowerInvariant()}:{payload.Trim()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string? MergeNotes(string? existingNotes, string? newNotes)
    {
        if (string.IsNullOrWhiteSpace(newNotes))
            return existingNotes;

        if (string.IsNullOrWhiteSpace(existingNotes))
            return newNotes.Trim();

        return $"{existingNotes.Trim()}\n{newNotes.Trim()}";
    }
}
