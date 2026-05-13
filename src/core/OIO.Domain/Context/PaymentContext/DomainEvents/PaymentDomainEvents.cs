using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.PaymentContext.DomainEvents;

// ─── Transaction Events ──────────────────────────────────────────────

public sealed record TransactionCompletedDomainEvent(
    TransactionId TransactionId,
    UserId UserId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record TransactionFailedDomainEvent(
    TransactionId TransactionId,
    UserId UserId,
    decimal Amount,
    string Currency,
    string TransactionType,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record TransactionRefundedDomainEvent(
    TransactionId TransactionId,
    UserId UserId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

// ─── Wallet Events ───────────────────────────────────────────────────

public sealed record WalletCreditedDomainEvent(
    WalletId WalletId,
    UserId UserId,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WalletDebitedDomainEvent(
    WalletId WalletId,
    UserId UserId,
    decimal Amount,
    decimal BalanceAfter,
    string? Description,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WalletHeldDomainEvent(
    WalletId WalletId,
    UserId UserId,
    decimal Amount,
    string? Description,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WalletUnheldDomainEvent(
    WalletId WalletId,
    UserId UserId,
    decimal Amount,
    string? Description,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

// ─── Escrow Events ───────────────────────────────────────────────────

public sealed record EscrowReleasedToSellerDomainEvent(
    EscrowId EscrowId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record EscrowRefundedToBuyerDomainEvent(
    EscrowId EscrowId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record EscrowForfeitedToPlatformDomainEvent(
    EscrowId EscrowId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

// ─── Withdrawal Events ──────────────────────────────────────────────

public sealed record WithdrawalRequestCreatedDomainEvent(
    WithdrawalRequestId WithdrawalRequestId,
    UserId UserId,
    decimal Amount,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WithdrawalApprovedDomainEvent(
    WithdrawalRequestId WithdrawalRequestId,
    UserId UserId,
    UserId ApprovedBy,
    decimal Amount,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WithdrawalRejectedDomainEvent(
    WithdrawalRequestId WithdrawalRequestId,
    UserId UserId,
    UserId RejectedBy,
    string Reason,
    decimal Amount,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

public sealed record WithdrawalCompletedDomainEvent(
    WithdrawalRequestId WithdrawalRequestId,
    UserId UserId,
    decimal NetAmount,
    DateTime OccurredAt) : DomainEvent(OccurredAt);

// ─── Invoice Events ──────────────────────────────────────────────────

public sealed record InvoicePaidDomainEvent(
    InvoiceId InvoiceId,
    Guid OrderId,
    UserId BuyerId,
    decimal TotalAmount,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
