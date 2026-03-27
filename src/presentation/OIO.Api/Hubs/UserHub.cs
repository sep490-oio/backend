using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OIO.Application.Context.UserContext.Hubs;
using OIO.Application.Context.UserContext.Services;
using SignalRSwaggerGen.Attributes;

namespace OIO.Api.Hubs;

[SignalRHub("/hubs/user", tag: "Hub")]
[Authorize]
public sealed class UserHub : Hub<IUserHubClient>
{
    private readonly ICurrentUser _currentUser;

    public UserHub(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUser.UserId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId.Value}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.UserId;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId.Value}");
        await base.OnDisconnectedAsync(exception);
    }
}
