using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;

public sealed class WithdrawalRequest : AggregateRoot<WithdrawalRequestId>, ICreatedAtEntity
{
    public UserId UserId { get; private set; }
    public WalletId WalletId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Fee { get; private set; }
    public decimal NetAmount { get; private set; }
    public BankAccount BankAccount { get; private set; }
    public WithdrawalStatus Status { get; private set; }
    public UserId? ProcessedBy { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WithdrawalRequest() { }
}