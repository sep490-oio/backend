using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.UserContext.Aggregates.Users.Events;

/// <summary>
/// Raised when a <see cref="TermsDocument"/> transitions from <c>Draft</c> to <c>Active</c>.
/// Handlers fan out re-acceptance enforcement per ralplan §3.6:
/// cache invalidation → SignalR broadcast → outbox dispatch → metrics.
/// <para>
/// IDs are stringified for outbox stability and cross-context readability.
/// <paramref name="SupersededId"/> is populated by the activation command handler when it
/// archives a sibling of the same <see cref="TermType"/>; the aggregate itself does not know
/// about siblings.
/// </para>
/// </summary>
public sealed record TermsDocumentActivatedEvent(
    string TermsDocumentId,
    string TermType,
    int NewVersion,
    string? SupersededId,
    string ActivatedBy,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
