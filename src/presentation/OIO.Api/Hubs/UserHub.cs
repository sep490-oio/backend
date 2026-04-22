using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OIO.Api.Services;
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

        // Plan C4: authenticated users join the `terms:authenticated` group so that
        // TermsDocumentActivatedEventHandler's broadcast reaches every live session.
        // The [Authorize] attribute guarantees this branch only runs for authenticated users;
        // anonymous connections never reach the hub and therefore never join this group.
        if (_currentUser.IsAuthenticated)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId, TermsHubBroadcaster.AuthenticatedGroup);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUser.UserId;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId.Value}");

        if (_currentUser.IsAuthenticated)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId, TermsHubBroadcaster.AuthenticatedGroup);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
