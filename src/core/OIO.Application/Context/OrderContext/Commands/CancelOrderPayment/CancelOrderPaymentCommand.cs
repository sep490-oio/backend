using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.CancelOrderPayment;

public sealed record CancelOrderPaymentCommand(Guid OrderId, string? Reason) : ICommand;

internal sealed class CancelOrderPaymentCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IAuctionRealtimePublisher realtimePublisher,
    IClock clock,
    ILogger<CancelOrderPaymentCommandHandler> logger)
    : ICommandHandler<CancelOrderPaymentCommand>
{
    /// <summary>
    /// Deposit penalty rate for auction-win orders cancelled by the buyer.
    /// 50% of the deposit is forfeited as a penalty; 50% is returned.
    /// </summary>
    private const decimal DepositPenaltyRate = 0.5m;

    public async Task<UnitResult<Error>> Handle(
        CancelOrderPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;

        // ── 1. Load order ────────────────────────────────────────────────
        var order = await dbContext.GetByIdAsync<Order, OrderId>(
            OrderId.From(request.OrderId),
            queryBuilder: q => q.AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order not found.");

        if (order.BuyerId != currentUser.UserId)
            return Error.Forbidden(
                "Order.NotBuyer",
                "Only the buyer can cancel payment for this order.");

        if (order.Status != OrderStatus.PendingPayment)
            return Error.Conflict(
                "Order.CannotCancel",
                $"Order cannot be cancelled in status '{order.Status.Id}'.");

        // ── 2. Load auction with deposits & reservations ────────────────
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            order.AuctionId,
            queryBuilder: q => q
                .Include(a => a.Bids)
                .Include(a => a.WinnerOffers)
                .Include(a => a.Deposits)
                .Include(a => a.BuyNowReservations)
                .Include(a => a.Item)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Linked auction not found.");

        // ── 3. Cancel the order ─────────────────────────────────────────
        var reason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Buyer cancelled payment"
            : request.Reason.Trim();

        var cancelResult = order.Cancel(reason, nowUtc);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        // ── 4. Determine order type & apply consequences ─────────────────
        //
        // Buy-now order: has a PendingPayment reservation linked to this buyer.
        // Auction-win order: the buyer is the auction winner.
        //
        var buyNowReservation = auction.BuyNowReservations
            .FirstOrDefault(r => r.BuyerId == order.BuyerId && r.IsPendingPayment);

        if (buyNowReservation is not null)
        {
            // ── BUY-NOW CANCEL ──────────────────────────────────────
            // Fail reservation → triggers ApplyBuyNowCompensation (domain event)
            // → BuyNowCompensationTimerRescheduleHandler reschedules Quartz timers.
            // Deposit is fully returned (no penalty for buy-now).
            var failResult = auction.FailBuyNowReservation(
                buyNowReservation.Id,
                $"Buyer cancelled payment for order {order.OrderNumber.Value}",
                nowUtc);

            if (failResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to release buy-now reservation {ReservationId} on cancel: {Error}",
                    buyNowReservation.Id.Value, failResult.Error.Message);
            }

            logger.LogInformation(
                "Buy-now order {OrderId} cancelled by buyer. Reservation {ReservationId} released.",
                request.OrderId, buyNowReservation.Id.Value);
        }
        else
        {
            // ── AUCTION-WIN CANCEL ──────────────────────────────────
            // 50% deposit penalty: forfeit half, return half.
            // Then trigger MarkPaymentDefaulted → runner-up fallback flow.
            var winnerDeposit = auction.Deposits
                .FirstOrDefault(d =>
                    d.BidderId == order.BuyerId && d.IsHeld);

            if (winnerDeposit is not null)
            {
                await ApplyPartialDepositPenaltyAsync(
                    winnerDeposit, DepositPenaltyRate, nowUtc, cancellationToken);
            }

            var defaultResult = auction.MarkPaymentDefaulted(nowUtc);
            if (defaultResult.IsFailure)
            {
                logger.LogWarning(
                    "MarkPaymentDefaulted failed for auction {AuctionId} on cancel: {Error}",
                    auction.Id.Value, defaultResult.Error.Message);
                // Don't fail the whole operation — order is already cancelled.
                // The fallback job will pick up the defaulted state.
            }

            logger.LogInformation(
                "Auction-win order {OrderId} cancelled by buyer. " +
                "Deposit penalty ({PenaltyRate:P0}) applied. Auction {AuctionId} marked as payment defaulted.",
                request.OrderId, DepositPenaltyRate, auction.Id.Value);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // ── 5. Broadcast state change via SignalR ────────────────────────
        // Must happen AFTER commit so clients see consistent data.
        try
        {
            if (buyNowReservation is not null)
            {
                await realtimePublisher.PublishBuyNowReservationReleasedAsync(
                    auction.Id.Value,
                    new BuyNowReservationReleasedNotification(
                        AuctionId: auction.Id.Value,
                        ReservationId: buyNowReservation.Id.Value,
                        BuyerId: order.BuyerId.Value,
                        Reason: reason,
                        ReleasedAt: nowUtc),
                    cancellationToken);
            }
            else
            {
                // Auction-win cancel: broadcast state change (status → PaymentDefaulted)
                await realtimePublisher.PublishStateChangedAsync(
                    auction.Id.Value, ct: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Non-critical — order is already cancelled.
            logger.LogWarning(ex,
                "Failed to broadcast realtime update after cancel payment for order {OrderId}",
                request.OrderId);
        }

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Applies a partial deposit penalty: forfeits <paramref name="penaltyRate"/> of the
    /// deposit (permanently removed from wallet) and returns the remainder to available balance.
    /// </summary>
    private async Task ApplyPartialDepositPenaltyAsync(
        AuctionDeposit deposit,
        decimal penaltyRate,
        DateTime nowUtc,
        CancellationToken ct)
    {
        var wallet = await dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == deposit.BidderId, ct);

        if (wallet is null)
        {
            logger.LogError(
                "Wallet not found for user {UserId} during deposit penalty. DepositId={DepositId}",
                deposit.BidderId.Value, deposit.Id.Value);
            return;
        }

        var totalAmount = deposit.Amount.Amount;
        var penaltyAmount = Math.Round(totalAmount * penaltyRate, 2);
        var returnAmount = totalAmount - penaltyAmount;

        // Mark deposit as forfeited at the domain level
        var forfeitResult = deposit.Forfeit(nowUtc);
        if (forfeitResult.IsFailure)
        {
            logger.LogWarning(
                "Deposit.Forfeit failed for {DepositId}: {Error}",
                deposit.Id.Value, forfeitResult.Error.Message);
            return;
        }

        // Penalty portion: permanently remove from pending balance
        if (penaltyAmount > 0)
        {
            var debitResult = wallet.DebitPending(
                amount: penaltyAmount,
                transactionId: deposit.TransactionId,
                description: LedgerDescriptions.DepositPenalty(penaltyRate),
                nowUtc: nowUtc);

            if (debitResult.IsFailure)
            {
                logger.LogWarning(
                    "Wallet.DebitPending failed for penalty portion: {Error}",
                    debitResult.Error.Message);
            }
        }

        // Return portion: move from pending to available balance
        if (returnAmount > 0)
        {
            var unholdResult = wallet.Unhold(
                amount: returnAmount,
                transactionId: deposit.TransactionId,
                description: LedgerDescriptions.PartialDepositReturn(1 - penaltyRate),
                nowUtc: nowUtc);

            if (unholdResult.IsFailure)
            {
                logger.LogWarning(
                    "Wallet.Unhold failed for return portion: {Error}",
                    unholdResult.Error.Message);
            }
        }

        logger.LogInformation(
            "Deposit penalty applied: DepositId={DepositId}, Total={Total}, Penalty={Penalty}, Returned={Returned}",
            deposit.Id.Value, totalAmount, penaltyAmount, returnAmount);
    }
}
