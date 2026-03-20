using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.PaymentContext.DomainEvents;

namespace OIO.Application.Context.PaymentContext.EventHandlers;

/// <summary>
/// Ghi audit log cho các hành động tài chính quan trọng.
/// Handler nhận DomainEvent qua Outbox → MediatR pipeline.
/// </summary>

// ─── Transaction Completed ───────────────────────────────────────────
internal sealed class TransactionCompletedAuditHandler
    : INotificationHandler<TransactionCompletedDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<TransactionCompletedAuditHandler> _logger;

    public TransactionCompletedAuditHandler(IDbContext dbContext, ILogger<TransactionCompletedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(TransactionCompletedDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: notification.UserId,
            actorRole: "system",
            action: "TransactionCompleted",
            entityType: "Transaction",
            entityId: notification.TransactionId.Value,
            oldData: null,
            newData: $"{{\"amount\":{notification.Amount},\"currency\":\"{notification.Currency}\",\"type\":\"{notification.TransactionType}\"}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogInformation(
            "Audit: Transaction {TransactionId} completed. Amount: {Amount} {Currency}, Type: {Type}",
            notification.TransactionId.Value, notification.Amount, notification.Currency, notification.TransactionType);

        return Task.CompletedTask;
    }
}

// ─── Transaction Failed ──────────────────────────────────────────────
internal sealed class TransactionFailedAuditHandler
    : INotificationHandler<TransactionFailedDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<TransactionFailedAuditHandler> _logger;

    public TransactionFailedAuditHandler(IDbContext dbContext, ILogger<TransactionFailedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(TransactionFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: notification.UserId,
            actorRole: "system",
            action: "TransactionFailed",
            entityType: "Transaction",
            entityId: notification.TransactionId.Value,
            oldData: null,
            newData: $"{{\"amount\":{notification.Amount},\"currency\":\"{notification.Currency}\",\"type\":\"{notification.TransactionType}\"}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogWarning(
            "Audit: Transaction {TransactionId} FAILED. Amount: {Amount} {Currency}, Type: {Type}",
            notification.TransactionId.Value, notification.Amount, notification.Currency, notification.TransactionType);

        return Task.CompletedTask;
    }
}

// ─── Withdrawal Approved ─────────────────────────────────────────────
internal sealed class WithdrawalApprovedAuditHandler
    : INotificationHandler<WithdrawalApprovedDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<WithdrawalApprovedAuditHandler> _logger;

    public WithdrawalApprovedAuditHandler(IDbContext dbContext, ILogger<WithdrawalApprovedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(WithdrawalApprovedDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: notification.ApprovedBy,
            actorRole: "admin",
            action: "WithdrawalApproved",
            entityType: "WithdrawalRequest",
            entityId: notification.WithdrawalRequestId.Value,
            oldData: "{\"status\":\"pending\"}",
            newData: $"{{\"status\":\"approved\",\"amount\":{notification.Amount}}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogInformation(
            "Audit: Withdrawal {WithdrawalId} approved by admin {AdminId}. Amount: {Amount}",
            notification.WithdrawalRequestId.Value, notification.ApprovedBy.Value, notification.Amount);

        return Task.CompletedTask;
    }
}

// ─── Withdrawal Rejected ─────────────────────────────────────────────
internal sealed class WithdrawalRejectedAuditHandler
    : INotificationHandler<WithdrawalRejectedDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<WithdrawalRejectedAuditHandler> _logger;

    public WithdrawalRejectedAuditHandler(IDbContext dbContext, ILogger<WithdrawalRejectedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(WithdrawalRejectedDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: notification.RejectedBy,
            actorRole: "admin",
            action: "WithdrawalRejected",
            entityType: "WithdrawalRequest",
            entityId: notification.WithdrawalRequestId.Value,
            oldData: "{\"status\":\"pending\"}",
            newData: $"{{\"status\":\"rejected\",\"reason\":\"{notification.Reason}\"}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogWarning(
            "Audit: Withdrawal {WithdrawalId} rejected by admin {AdminId}. Reason: {Reason}",
            notification.WithdrawalRequestId.Value, notification.RejectedBy.Value, notification.Reason);

        return Task.CompletedTask;
    }
}

// ─── Escrow Released (Seller Payout) ─────────────────────────────────
internal sealed class EscrowReleasedAuditHandler
    : INotificationHandler<EscrowReleasedToSellerDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<EscrowReleasedAuditHandler> _logger;

    public EscrowReleasedAuditHandler(IDbContext dbContext, ILogger<EscrowReleasedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(EscrowReleasedToSellerDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: null,
            actorRole: "system",
            action: "EscrowReleasedToSeller",
            entityType: "Escrow",
            entityId: notification.EscrowId.Value,
            oldData: "{\"status\":\"holding\"}",
            newData: $"{{\"status\":\"released_to_seller\",\"amount\":{notification.Amount},\"orderId\":\"{notification.OrderId}\"}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogInformation(
            "Audit: Escrow {EscrowId} released to seller. Amount: {Amount} {Currency}",
            notification.EscrowId.Value, notification.Amount, notification.Currency);

        return Task.CompletedTask;
    }
}

// ─── Escrow Refunded (Buyer Refund) ──────────────────────────────────
internal sealed class EscrowRefundedAuditHandler
    : INotificationHandler<EscrowRefundedToBuyerDomainEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<EscrowRefundedAuditHandler> _logger;

    public EscrowRefundedAuditHandler(IDbContext dbContext, ILogger<EscrowRefundedAuditHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public Task Handle(EscrowRefundedToBuyerDomainEvent notification, CancellationToken cancellationToken)
    {
        var audit = AuditLog.Create(
            actorUserId: null,
            actorRole: "system",
            action: "EscrowRefundedToBuyer",
            entityType: "Escrow",
            entityId: notification.EscrowId.Value,
            oldData: "{\"status\":\"holding\"}",
            newData: $"{{\"status\":\"refunded_to_buyer\",\"amount\":{notification.Amount},\"orderId\":\"{notification.OrderId}\"}}",
            ipAddress: null,
            nowUtc: notification.OccurredAt);

        _dbContext.Set<AuditLog>().Add(audit);

        _logger.LogInformation(
            "Audit: Escrow {EscrowId} refunded to buyer. Amount: {Amount} {Currency}",
            notification.EscrowId.Value, notification.Amount, notification.Currency);

        return Task.CompletedTask;
    }
}
