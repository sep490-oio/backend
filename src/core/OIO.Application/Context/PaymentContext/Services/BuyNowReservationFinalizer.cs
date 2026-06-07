using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Services;

/// <summary>
/// Shared finalizer for Buy Now reservations after an order has been successfully paid.
/// Centralises behaviour previously duplicated between CheckoutOrderCommand (full-wallet path)
/// and ProcessVnPayCallbackCommand (VnPay OrderPayment path), including the deposit-funding
/// ledger entries for reservations that consumed an existing auction deposit.
/// </summary>
internal sealed class BuyNowReservationFinalizer
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<BuyNowReservationFinalizer> _logger;

    public BuyNowReservationFinalizer(
        IDbContext dbContext,
        ILogger<BuyNowReservationFinalizer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Finalises the buy-now reservation linked to <paramref name="order"/> (if any) and applies
    /// any outstanding deposit-funding ledger entries. Safe no-op for non-buy-now orders.
    /// </summary>
    public async Task<UnitResult<Error>> FinalizeAsync(
        Order order,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var reservation = await _dbContext.Set<AuctionBuyNowReservation>()
            .FirstOrDefaultAsync(
                r => r.OrderId == order.Id && r.Status == BuyNowReservationStatus.PendingPayment,
                cancellationToken);

        if (reservation is null)
            return UnitResult.Success<Error>();

        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
            .Include(a => a.Bids)
            .Include(a => a.AutoBids)
            .Include(a => a.Deposits)
            .Include(a => a.Participants)
            .Include(a => a.BuyNowReservations)
            .FirstOrDefaultAsync(a => a.Id == reservation.AuctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(reservation.AuctionId);

        var finalizeResult = auction.FinalizeBuyNowReservation(reservation.Id, nowUtc);
        if (finalizeResult.IsFailure)
        {
            _logger.LogError(
                "Failed to finalize buy-now reservation {ReservationId} for order {OrderId}: {Error}",
                reservation.Id.Value,
                order.Id.Value,
                finalizeResult.Error.Message);
            return finalizeResult.Error;
        }

        if (reservation.DepositAppliedAmount.Amount > 0)
        {
            var depositFundingResult = await ApplyBuyNowDepositFundingAsync(
                auction,
                order,
                reservation,
                nowUtc,
                cancellationToken);

            if (depositFundingResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to apply buy-now deposit funding for reservation {ReservationId}: {Error}",
                    reservation.Id.Value,
                    depositFundingResult.Error.Message);
                return depositFundingResult.Error;
            }
        }

        _logger.LogInformation(
            "Buy-now reservation {ReservationId} finalized and auction marked Sold for order {OrderId}",
            reservation.Id.Value,
            order.Id.Value);

        return UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> ApplyBuyNowDepositFundingAsync(
        Auction auction,
        Order order,
        AuctionBuyNowReservation reservation,
        DateTime now,
        CancellationToken ct)
    {
        var deposit = auction.Deposits
            .FirstOrDefault(x => x.BidderId == reservation.BuyerId && x.IsHeld);

        if (deposit is null)
            return Error.NotFound("AuctionDeposit.NotFound", "Held buyer deposit not found for buy-now funding.");

        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.UserId == reservation.BuyerId, ct);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Buyer wallet not found for buy-now deposit funding.");

        var txNumberResult = TransactionNumber.Create($"BNDEP-{Guid.CreateVersion7():N}");
        if (txNumberResult.IsFailure)
            return txNumberResult.Error;

        var fundingTx = Transaction.Create(
            userId: reservation.BuyerId,
            transactionNumber: txNumberResult.Value,
            type: TransactionType.Payment,
            amount: reservation.DepositAppliedAmount,
            currency: reservation.DepositAppliedAmount.Currency.Id,
            description: LedgerDescriptions.BuyNowDepositApplied(auction.Id.Value, reservation.Id.Value, order.Id.Value),
            nowUtc: now,
            orderId: order.Id,
            auctionId: auction.Id,
            buyNowReservationId: reservation.Id);

        if (fundingTx.IsFailure)
            return fundingTx.Error;

        var markCompletedResult = fundingTx.Value.MarkAsCompleted(GatewayInfo.Empty, now);
        if (markCompletedResult.IsFailure)
            return markCompletedResult.Error;

        _dbContext.Insert(fundingTx.Value);

        var convertResult = deposit.ConvertToPayment(now);
        if (convertResult.IsFailure)
            return convertResult.Error;

        var debitPendingResult = wallet.DebitPending(
            reservation.DepositAppliedAmount.Amount,
            fundingTx.Value.Id,
            LedgerDescriptions.BuyNowDepositAppliedToWallet(reservation.Id.Value),
            now);

        if (debitPendingResult.IsFailure)
        {
            var debitResult = wallet.Debit(
                reservation.DepositAppliedAmount.Amount,
                fundingTx.Value.Id,
                LedgerDescriptions.BuyNowDepositAppliedToWallet(reservation.Id.Value),
                now);

            if (debitResult.IsFailure)
                return debitPendingResult.Error;
        }

        var escrowResult = Escrow.Create(
            order.Id,
            fundingTx.Value.Id,
            reservation.DepositAppliedAmount,
            reservation.DepositAppliedAmount.Currency.Id,
            now);

        if (escrowResult.IsFailure)
            return escrowResult.Error;

        _dbContext.Insert(escrowResult.Value);

        return UnitResult.Success<Error>();
    }
}
