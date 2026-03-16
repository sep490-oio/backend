using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Application.Context.ModerationContext.Services;

public interface IDisputeRealtimeService
{
    Task BroadcastMessageAsync(Guid disputeId, DisputeMessageDto message, bool adminOnly, CancellationToken cancellationToken = default);
    Task BroadcastReadStateAsync(Guid disputeId, DisputeParticipantReadStateDto state, bool adminOnly, CancellationToken cancellationToken = default);
    Task BroadcastDisputeUpdatedAsync(Guid disputeId, DisputeThreadMetaDto meta, bool adminOnly, CancellationToken cancellationToken = default);
    Task BroadcastUnreadUpdatedAsync(Guid userId, DisputeUnreadUpdateDto update, CancellationToken cancellationToken = default);
}
