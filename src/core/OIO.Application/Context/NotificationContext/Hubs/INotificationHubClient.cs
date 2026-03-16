namespace OIO.Application.Context.NotificationContext.Hubs;

public interface INotificationHubClient
{
    Task ReceiveNotification(NotificationPushDto notification);
    Task UnreadCountUpdated(int count);
}

public sealed record NotificationPushDto(
    Guid NotificationId,
    string NotificationType,
    string EventType,
    string Title,
    string Message,
    string? EntityType,
    Guid? EntityId,
    string Priority,
    DateTime CreatedAt);
