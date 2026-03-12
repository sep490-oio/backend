using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeRefund : BaseEntity<DisputeRefundId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public TransactionId TransactionId { get; private set; }  // UNIQUE
    public RefundType RefundType { get; private set; }
    public string Reason { get; private set; }
    public UserId? ApprovedBy { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    private DisputeRefund() { }
}