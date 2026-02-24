using MediatR;

namespace OIO.Domain.SeedWork.DomainEvents;

public interface IDomainEvent : INotification
{
    Guid EventId { get; init; }
    DateTime OccurredAt { get; init; }
    string EventType { get; }
}