using Microsoft.AspNetCore.SignalR;
using OIO.Api.Hubs;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Hubs;
using OIO.Application.Context.ModerationContext.Services;

namespace OIO.Api.Services;

internal sealed class DisputeRealtimeService(
    IHubContext<DisputeHub, IDisputeHubClient> hubContext)
    : IDisputeRealtimeService
{
    public Task BroadcastMessageAsync(
        Guid disputeId,
        DisputeMessageDto message,
        bool adminOnly,
        CancellationToken cancellationToken = default)
    {
        return adminOnly
            ? hubContext.Clients.Group(DisputeHub.AdminRoomGroupName(disputeId)).MessageReceived(message)
            : hubContext.Clients.Group(DisputeHub.RoomGroupName(disputeId)).MessageReceived(message);
    }

    public Task BroadcastReadStateAsync(
        Guid disputeId,
        DisputeParticipantReadStateDto state,
        bool adminOnly,
        CancellationToken cancellationToken = default)
    {
        return adminOnly
            ? hubContext.Clients.Group(DisputeHub.AdminRoomGroupName(disputeId)).ReadStateUpdated(state)
            : hubContext.Clients.Group(DisputeHub.RoomGroupName(disputeId)).ReadStateUpdated(state);
    }

    public Task BroadcastDisputeUpdatedAsync(
        Guid disputeId,
        DisputeThreadMetaDto meta,
        bool adminOnly,
        CancellationToken cancellationToken = default)
    {
        return adminOnly
            ? hubContext.Clients.Group(DisputeHub.AdminRoomGroupName(disputeId)).DisputeUpdated(meta)
            : hubContext.Clients.Group(DisputeHub.RoomGroupName(disputeId)).DisputeUpdated(meta);
    }

    public Task BroadcastUnreadUpdatedAsync(
        Guid userId,
        DisputeUnreadUpdateDto update,
        CancellationToken cancellationToken = default)
    {
        return hubContext.Clients.Group(DisputeHub.UserGroupName(userId)).DisputeUnreadUpdated(update);
    }
}
