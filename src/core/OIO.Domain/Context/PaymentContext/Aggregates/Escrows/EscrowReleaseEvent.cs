using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Escrows;

public sealed class EscrowReleaseEvent : BaseEntity<EscrowReleaseEventId>, ICreatedAtEntity
{
    public EscrowId EscrowId { get; private set; }
    public EscrowReleaseType ReleaseType { get; private set; }
    public string TriggerSourceType { get; private set; }
    public Guid? TriggerSourceId { get; private set; }
    public decimal Amount { get; private set; }
    public UserId? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Escrow Escrow { get; private set; } = null!;

    private EscrowReleaseEvent() { }
}