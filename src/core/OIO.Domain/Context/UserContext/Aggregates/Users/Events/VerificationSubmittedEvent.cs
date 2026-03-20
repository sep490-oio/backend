using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record VerificationSubmittedEvent(
    Guid VerificationId,
    Guid UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
