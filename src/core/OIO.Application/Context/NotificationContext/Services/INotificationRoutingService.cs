using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.NotificationContext.Services;

public interface INotificationRoutingService
{
    IEnumerable<NotificationDelivery> Route(Notification notification, UserNotificationPreference? preference);
}
