using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.NotificationContext.Commands.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid UserId,
    string NotificationType,
    string EventType,
    string Title,
    string Message,
    NotificationPriority? Priority = null,
    string? EntityType = null,
    Guid? EntityId = null,
    string? Metadata = null,
    string? RelatedEntities = null,
    string? Actions = null,
    DateTime? ExpiresAt = null) : ICommand<Guid>;
