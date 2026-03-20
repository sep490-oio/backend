using System.Text.Json;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.NotificationContext.Services;

internal sealed class NotificationRoutingService : INotificationRoutingService
{
    public IEnumerable<NotificationDelivery> Route(Notification notification, UserNotificationPreference? preference)
    {
        var deliveries = new List<NotificationDelivery>();
        var now = DateTime.UtcNow;

        var enabledChannels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (preference is not null && preference.IsEnabled)
        {
            try 
            {
                if (!string.IsNullOrWhiteSpace(preference.Channels))
                {
                    var channels = JsonSerializer.Deserialize<string[]>(preference.Channels);
                    if (channels is not null)
                    {
                        foreach(var c in channels)
                            enabledChannels.Add(c);
                    }
                    else
                    {
                        enabledChannels.Add("Email");
                        enabledChannels.Add("SignalR");
                    }
                }
                else
                {
                    enabledChannels.Add("Email");
                    enabledChannels.Add("SignalR");
                }
            }
            catch 
            {
                enabledChannels.Add("Email");
                enabledChannels.Add("SignalR");
            }
        }
        else if (preference is null)
        {
            // Opt-in by default
            enabledChannels.Add("Email");
            enabledChannels.Add("SignalR");
        }

        if (enabledChannels.Contains("Email"))
        {
            deliveries.Add(NotificationDelivery.Create(
                notification.Id, 
                notification.UserId.Value, 
                NotificationChannel.Email, 
                now, 
                maxAttempts: 3));
        }

        if (enabledChannels.Contains("SignalR"))
        {
            deliveries.Add(NotificationDelivery.Create(
                notification.Id, 
                notification.UserId.Value, 
                NotificationChannel.SignalR, 
                now, 
                maxAttempts: 1));
        }

        return deliveries;
    }
}
