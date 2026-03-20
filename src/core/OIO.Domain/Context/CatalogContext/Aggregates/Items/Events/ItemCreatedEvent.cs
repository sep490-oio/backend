using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.CatalogContext.Aggregates.Items.Events;

public sealed record ItemCreatedEvent(
    string ItemId,
    string SellerId,
    string Title,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record ItemStatusChangedEvent(
    string ItemId,
    string OldStatus,
    string NewStatus,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record ItemQuestionAskedEvent(
    string ItemId,
    string QuestionId,
    string AskerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record ItemQuestionAnsweredEvent(
    string ItemId,
    string QuestionId,
    string AskerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record MediaRemovedFromItemEvent(
    string ItemId,
    string MediaId,
    string PublicId,
    string ResourceType,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record ItemSubmittedEvent(
    string ItemId,
    string AuctionId,
    bool VerifyByPlatform,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record ItemApprovedEvent(
    string ItemId,
    string AuctionId,
    string ReviewerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record ItemRejectedEvent(
    string ItemId,
    string AuctionId,
    string ReviewerId,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);