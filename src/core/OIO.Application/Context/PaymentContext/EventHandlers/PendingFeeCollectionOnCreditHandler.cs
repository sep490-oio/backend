using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.DomainEvents;

namespace OIO.Application.Context.PaymentContext.EventHandlers;

/// <summary>
/// Listens for <see cref="WalletCreditedDomainEvent"/> and attempts to
/// auto-collect any outstanding pending <c>Fee</c> transactions against the
/// credited user. This closes the gap where dispute-resolution charges
/// (inspection fee, commission) were deferred because the seller's wallet
/// had insufficient funds at the time of resolution.
///
/// The handler runs in the same unit-of-work as the credit operation, so
/// the fee debit is atomic with the deposit/payout that triggered the event.
/// </summary>
internal sealed class PendingFeeCollectionOnCreditHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    EscrowSettlementService escrowSettlementService,
    ILogger<PendingFeeCollectionOnCreditHandler> logger)
    : INotificationHandler<WalletCreditedDomainEvent>
{
    public async Task Handle(
        WalletCreditedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Platform wallets have no UserId — skip.
        if (notification.UserId.Value == Guid.Empty)
            return;

        // Look up the wallet to determine currency. The wallet entity is
        // already tracked in the current DbContext because the credit just
        // happened on it, so this doesn't cause an extra round-trip.
        var wallet = await dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(
                w => w.Id == notification.WalletId,
                cancellationToken);

        if (wallet is null)
        {
            logger.LogDebug(
                "PendingFeeCollection: wallet {WalletId} not found — skipping",
                notification.WalletId);
            return;
        }

        var currency = wallet.WalletFunds.Currency.Id;

        try
        {
            var collected = await escrowSettlementService.CollectPendingFeesAsync(
                notification.UserId,
                currency,
                cancellationToken);

            if (collected > 0)
            {
                // EscrowSettlementService mutates the change tracker (wallet debit, fee
                // completion, platform credit) but relies on its caller to persist. Domain
                // events are dispatched out-of-band via the outbox processor, whose scope
                // never flushes EF — so without this SaveChanges the settlement is silently
                // discarded and the fees stay Pending forever.
                await unitOfWork.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "PendingFeeCollection: auto-collected {Count} pending fee(s) from user {UserId} after wallet credit of {Amount} {Currency}",
                    collected,
                    notification.UserId,
                    notification.Amount,
                    currency);
            }
        }
        catch (Exception ex)
        {
            // Non-critical — log and swallow so the original credit operation
            // is not rolled back. The pending fees remain in Pending status
            // and will be retried on the next credit event.
            logger.LogWarning(ex,
                "PendingFeeCollection: failed to collect pending fees for user {UserId} after wallet credit — fees remain pending",
                notification.UserId);
        }
    }
}
