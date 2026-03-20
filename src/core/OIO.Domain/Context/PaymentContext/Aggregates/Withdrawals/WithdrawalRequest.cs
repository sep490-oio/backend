using OIO.Domain.Context.PaymentContext.DomainEvents;
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

    /// <summary>
    /// Tạo yêu cầu rút tiền mới. Tiền sẽ bị hold ở nơi gọi (Wallet.Hold).
    /// </summary>
    public static WithdrawalRequest Create(
        UserId userId,
        WalletId walletId,
        decimal amount,
        decimal fee,
        BankAccount bankAccount,
        DateTime nowUtc)
    {
        var withdrawal = new WithdrawalRequest
        {
            Id = WithdrawalRequestId.From(Guid.CreateVersion7()),
            UserId = userId,
            WalletId = walletId,
            Amount = amount,
            Fee = fee,
            NetAmount = amount - fee,
            BankAccount = bankAccount,
            Status = WithdrawalStatus.Pending,
            CreatedAt = nowUtc
        };

        withdrawal.RaiseDomainEvent(new WithdrawalRequestCreatedDomainEvent(
            withdrawal.Id, userId, amount, nowUtc));

        return withdrawal;
    }
    /// <summary>
    /// Admin duyệt yêu cầu rút tiền.
    /// </summary>
    public CSharpFunctionalExtensions.UnitResult<SeedWork.Errors.Error> Approve(UserId adminId, DateTime nowUtc)
    {
        if (Status != WithdrawalStatus.Pending)
            return SeedWork.Errors.Error.Conflict("Withdrawal.InvalidStatus",
                $"Cannot approve. Current status: {Status.Id}");

        Status = WithdrawalStatus.Approved;
        ProcessedBy = adminId;
        ProcessedAt = nowUtc;

        RaiseDomainEvent(new WithdrawalApprovedDomainEvent(
            Id, UserId, adminId, Amount, nowUtc));

        return CSharpFunctionalExtensions.UnitResult.Success<SeedWork.Errors.Error>();
    }

    /// <summary>
    /// Admin từ chối yêu cầu rút tiền. Tiền sẽ được unhold ở nơi gọi (Wallet.Unhold).
    /// </summary>
    public CSharpFunctionalExtensions.UnitResult<SeedWork.Errors.Error> Reject(UserId adminId, string reason, DateTime nowUtc)
    {
        if (Status != WithdrawalStatus.Pending)
            return SeedWork.Errors.Error.Conflict("Withdrawal.InvalidStatus",
                $"Cannot reject. Current status: {Status.Id}");

        Status = WithdrawalStatus.Rejected;
        ProcessedBy = adminId;
        RejectionReason = reason;
        ProcessedAt = nowUtc;

        RaiseDomainEvent(new WithdrawalRejectedDomainEvent(
            Id, UserId, adminId, reason, Amount, nowUtc));

        return CSharpFunctionalExtensions.UnitResult.Success<SeedWork.Errors.Error>();
    }

    /// <summary>
    /// Đánh dấu đang chuyển khoản qua gateway.
    /// </summary>
    public CSharpFunctionalExtensions.UnitResult<SeedWork.Errors.Error> MarkAsProcessing()
    {
        if (Status != WithdrawalStatus.Approved)
            return SeedWork.Errors.Error.Conflict("Withdrawal.InvalidStatus",
                $"Cannot mark as processing. Current status: {Status.Id}");

        Status = WithdrawalStatus.Processing;
        return CSharpFunctionalExtensions.UnitResult.Success<SeedWork.Errors.Error>();
    }

    /// <summary>
    /// Gateway chuyển khoản thành công. Tiền sẽ bị DebitPending ở nơi gọi (Wallet.DebitPending).
    /// </summary>
    public CSharpFunctionalExtensions.UnitResult<SeedWork.Errors.Error> MarkAsCompleted(DateTime nowUtc)
    {
        if (Status != WithdrawalStatus.Processing && Status != WithdrawalStatus.Approved)
            return SeedWork.Errors.Error.Conflict("Withdrawal.InvalidStatus",
                $"Cannot complete. Current status: {Status.Id}");

        Status = WithdrawalStatus.Completed;
        ProcessedAt = nowUtc;

        RaiseDomainEvent(new WithdrawalCompletedDomainEvent(
            Id, UserId, NetAmount, nowUtc));

        return CSharpFunctionalExtensions.UnitResult.Success<SeedWork.Errors.Error>();
    }

    /// <summary>
    /// Người dùng huỷ yêu cầu rút tiền (chỉ khi đang Pending). Tiền sẽ được unhold ở nơi gọi.
    /// </summary>
    public CSharpFunctionalExtensions.UnitResult<SeedWork.Errors.Error> Cancel(DateTime nowUtc)
    {
        if (Status != WithdrawalStatus.Pending)
            return SeedWork.Errors.Error.Conflict("Withdrawal.InvalidStatus",
                $"Cannot cancel. Current status: {Status.Id}");

        Status = WithdrawalStatus.Cancelled;
        ProcessedAt = nowUtc;
        return CSharpFunctionalExtensions.UnitResult.Success<SeedWork.Errors.Error>();
    }
}