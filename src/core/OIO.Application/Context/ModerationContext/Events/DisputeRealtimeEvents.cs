using MediatR;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.ModerationContext.Events;

public sealed record DisputeMessageSentEvent(
    DisputeId DisputeId,
    DisputeMessageId MessageId,
    UserId SenderId,
    bool IsInternal,
    DateTime OccurredAt) : INotification;

public sealed record DisputeReadStateUpdatedEvent(
    DisputeId DisputeId,
    UserId UserId,
    DisputeMessageId LastReadMessageId,
    DateTime ReadAt) : INotification;

public sealed record DisputeChangedEvent(
    DisputeId DisputeId,
    DateTime OccurredAt) : INotification;
