using MediatR;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Application.Context.ModerationContext.Events;

public sealed record DisputeMessageSentEvent(
    DisputeId DisputeId,
    DisputeMessageId MessageId,
    UserId SenderId,
    bool IsInternal,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record DisputeReadStateUpdatedEvent(
    DisputeId DisputeId,
    UserId UserId,
    DisputeMessageId LastReadMessageId,
    DateTime ReadAt) : DomainEvent(ReadAt);

public sealed record DisputeChangedEvent(
    DisputeId DisputeId,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
