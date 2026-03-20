namespace OIO.Application.Context.NotificationContext.Queries.GetMyNotifications;

public sealed record NotificationDto(
    Guid Id,
    string NotificationType,
    string EventType,
    string Title,
    string Message,
    string Priority,
    string Status,
    string? EntityType,
    Guid? EntityId,
    string? Metadata,
    string? RelatedEntities,
    string? Actions,
    DateTime CreatedAt,
    DateTime? ReadAt,
    DateTime? ExpiresAt);
