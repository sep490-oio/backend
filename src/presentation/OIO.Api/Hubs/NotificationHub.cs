using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OIO.Application.Context.NotificationContext.Hubs;
using OIO.Application.Context.UserContext.Services;
using SignalRSwaggerGen.Attributes;
using OIO.Api.Common;

namespace OIO.Api.Hubs;

[SignalRHub("/hubs/notifications", tag: ApiEndpoint.Tags.Hub)]
[Authorize]
public sealed class NotificationHub : Hub<INotificationHubClient>
{
    private readonly ICurrentUser _currentUser;

    public NotificationHub(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.UserId;
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            UserGroupName(userId.Value));

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.UserId;
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            UserGroupName(userId.Value));

        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroupName(Guid userId) => $"notifications:{userId}";
}
