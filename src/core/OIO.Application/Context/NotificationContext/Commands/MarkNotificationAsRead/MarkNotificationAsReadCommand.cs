using OIO.Application.Abstractions.Messaging;

namespace OIO.Application.Context.NotificationContext.Commands.MarkNotificationAsRead;

public sealed record MarkNotificationAsReadCommand(
    Guid NotificationId) : ICommand<MarkNotificationAsReadResponse>;

public sealed record MarkNotificationAsReadResponse(Guid NotificationId);
