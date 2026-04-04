namespace OIO.Application.Context.ModerationContext.DTOs;

public sealed record ReportDto(
    Guid Id,
    Guid ReporterId,
    string EntityType,
    Guid EntityId,
    string ReasonCode,
    string? Description,
    string? Attachments,
    string Status,
    Guid? AssignedTo,
    DateTime CreatedAt,
    DateTime? AssignedAt,
    DateTime? ResolvedAt,
    DateTime? EscalatedEmergencyAt,
    string? ResolutionNotes,
    Guid? DisputeId);
