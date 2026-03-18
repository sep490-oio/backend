using CSharpFunctionalExtensions;
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

    private EscrowReleaseEvent(
        EscrowReleaseEventId id,
        EscrowId escrowId,
        EscrowReleaseType releaseType,
        string triggerSourceType,
        Guid? triggerSourceId,
        decimal amount,
        UserId? createdBy,
        DateTime now)
        : base(id)
    {
        EscrowId = escrowId;
        ReleaseType = releaseType;
        TriggerSourceType = triggerSourceType;
        TriggerSourceId = triggerSourceId;
        Amount = amount;
        CreatedBy = createdBy;
        CreatedAt = now;
    }

    internal static Result<EscrowReleaseEvent, SeedWork.Errors.Error> Create(
        EscrowId escrowId,
        EscrowReleaseType releaseType,
        string triggerSourceType,
        Guid? triggerSourceId,
        decimal amount,
        UserId? createdBy,
        DateTime now)
    {
        return Result.Success<EscrowReleaseEvent, SeedWork.Errors.Error>(
            new EscrowReleaseEvent(
                EscrowReleaseEventId.From(Guid.CreateVersion7()),
                escrowId,
                releaseType,
                triggerSourceType,
                triggerSourceId,
                amount,
                createdBy,
                now));
    }
}