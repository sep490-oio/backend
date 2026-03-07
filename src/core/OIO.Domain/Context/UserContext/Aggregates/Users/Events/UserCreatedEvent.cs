using System.Net;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

public sealed record UserCreatedEvent(
    string UserId,
    string UserName,
    string Email,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    