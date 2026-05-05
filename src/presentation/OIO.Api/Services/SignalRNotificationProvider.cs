using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OIO.Api.Hubs;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext.Hubs;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.NotificationContext.Aggregates;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Api.Services;

internal sealed class SignalRNotificationProvider : INotificationProvider
{
    private readonly IHubContext<NotificationHub, INotificationHubClient> _hubContext;
    private readonly IDbContext _dbContext;

    public SignalRNotificationProvider(
        IHubContext<NotificationHub, INotificationHubClient> hubContext,
        IDbContext dbContext)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
    }

    public string ChannelType => "SignalR";

    public async Task<Result> SendAsync(
        Notification notification,
        NotificationDelivery delivery,
        CancellationToken ct = default)
    {
        try
        {
            var dto = new NotificationPushDto(
                notification.Id.Value,
                notification.NotificationType,
                notification.EventType,
                notification.Title,
                notification.Message,
                notification.EntityType,
                notification.EntityId,
                notification.Priority.Id,
                notification.CreatedAt);

            var groupName = NotificationHub.UserGroupName(notification.UserId.Value);
            await _hubContext.Clients.Group(groupName).ReceiveNotification(dto);

            var unreadCount = await _dbContext.Set<Notification>()
                .AsNoTracking()
                .CountAsync(
                    n => n.UserId == notification.UserId && n.Status == NotificationStatus.Unread,
                    ct);

            await _hubContext.Clients.Group(groupName).UnreadCountUpdated(unreadCount);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
