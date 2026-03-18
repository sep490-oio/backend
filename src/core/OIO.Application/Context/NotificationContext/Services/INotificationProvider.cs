using CSharpFunctionalExtensions;
using OIO.Domain.Context.NotificationContext.Aggregates;

namespace OIO.Application.Context.NotificationContext.Services;

public interface INotificationProvider
{
    string ChannelType { get; }
    Task<Result> SendAsync(Notification notification, NotificationDelivery delivery, CancellationToken ct = default);
}
