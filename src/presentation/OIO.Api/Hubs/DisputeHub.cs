using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.ModerationContext.Hubs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using SignalRSwaggerGen.Attributes;

namespace OIO.Api.Hubs;

[SignalRHub("/hubs/disputes", tag: ApiEndpoint.Tags.Hub)]
[Authorize]
public sealed class DisputeHub(
    ICurrentUser currentUser,
    DisputeAccessService accessService,
    IUnitOfWork unitOfWork)
    : Hub<IDisputeHubClient>
{
    public async Task JoinDispute(Guid disputeId)
    {
        var disputeResult = await accessService.GetAccessibleDisputeAsync(
            OIO.Domain.Context.ModerationContext.ValueObjects.Ids.DisputeId.From(disputeId),
            cancellationToken: Context.ConnectionAborted);

        if (disputeResult.IsFailure)
            throw new HubException(disputeResult.Error.Message);

        await accessService.EnsureParticipantStateAsync(disputeResult.Value.Id, Context.ConnectionAborted);
        await unitOfWork.SaveChangesAsync(Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId, RoomGroupName(disputeId), Context.ConnectionAborted);

        if (accessService.CanViewInternalMessages)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                AdminRoomGroupName(disputeId),
                Context.ConnectionAborted);
        }
    }

    public async Task LeaveDispute(Guid disputeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RoomGroupName(disputeId), Context.ConnectionAborted);

        if (accessService.CanViewInternalMessages)
        {
            await Groups.RemoveFromGroupAsync(
                Context.ConnectionId,
                AdminRoomGroupName(disputeId),
                Context.ConnectionAborted);
        }
    }

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            UserGroupName(currentUser.UserId.Value),
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            UserGroupName(currentUser.UserId.Value),
            Context.ConnectionAborted);

        await base.OnDisconnectedAsync(exception);
    }

    public static string RoomGroupName(Guid disputeId) => $"dispute:{disputeId}";
    public static string AdminRoomGroupName(Guid disputeId) => $"dispute:{disputeId}:admins";
    public static string UserGroupName(Guid userId) => $"dispute-user:{userId}";
}
