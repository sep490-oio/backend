using OIO.Application.Context.ModerationContext.DTOs;

namespace OIO.Application.Context.ModerationContext.Hubs;

public interface IDisputeHubClient
{
    Task MessageReceived(DisputeMessageDto message);
    Task ReadStateUpdated(DisputeParticipantReadStateDto state);
    Task DisputeUpdated(DisputeThreadMetaDto meta);
    Task DisputeUnreadUpdated(DisputeUnreadUpdateDto update);
}
