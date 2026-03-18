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
    DateTime CreatedAt);
