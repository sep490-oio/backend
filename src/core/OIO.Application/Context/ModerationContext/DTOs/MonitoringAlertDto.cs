namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record MonitoringAlertDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string AlertType,
    string Severity,
    string Payload,
    string Status,
    string? Notes,
    Guid? AcknowledgedBy,
    DateTime? AcknowledgedAt,
    Guid? ResolvedBy,
    DateTime? ResolvedAt,
    DateTime CreatedAt,
    string AlertTitle,
    int? Score,
    string Summary,
    long AgeSeconds,
    MonitoringPrimaryEntityDto PrimaryEntity,
    IReadOnlyList<MonitoringParticipantDto> Participants,
    IReadOnlyList<MonitoringEvidenceRefDto> EvidenceRefs,
    string? WindowLabel,
    string RecommendedNextStep,
    bool RawPayloadAvailable,
    Guid? AssignedTo,
    DateTime? AssignedAt,
    DateTime? SlaDueAt,
    bool IsOverdue,
    string? ResolutionOutcome,
    string? ResolutionReason,
    string? Fingerprint);

public sealed record MonitoringPrimaryEntityDto(
    string Type,
    Guid Id,
    string? DisplayName,
    string? Url);

public sealed record MonitoringParticipantDto(
    string Role,
    Guid UserId,
    string? DisplayName);

public sealed record MonitoringEvidenceRefDto(
    string Type,
    string IdOrValue,
    string Label,
    string? Route,
    string CopyValue,
    string Group,
    string Description);
