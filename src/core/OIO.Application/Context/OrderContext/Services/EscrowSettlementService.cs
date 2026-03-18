using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

public sealed class EscrowSettlementService
{
    private static readonly UserId SystemActorId = UserId.From(Guid.Empty);

    private readonly IDbContext _dbContext;
    private readonly IClock _clock;

    public EscrowSettlementService(IDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> ReleaseToSellerAsync(
        Order order,
        string reason,
        UserId? actorId,
        CancellationToken cancellationToken)
    {
        var escrows = await _dbContext.Set<Escrow>()
            .Where(x => x.OrderId == order.Id && x.Status == EscrowStatus.Holding)
            .ToListAsync(cancellationToken);

        if (escrows.Count == 0)
            return OrderErrors.Order.EscrowNotFound(order.Id);

        var sellerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.UserId == order.SellerId && x.IsActive, cancellationToken);

        if (sellerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

        var txNumber = TransactionNumber.Create($"PAYOUT-{Guid.CreateVersion7():N}");
        if (txNumber.IsFailure)
            return txNumber.Error;

        var totalAmount = escrows.Sum(x => x.Amount.Amount);
        var payoutMoney = Money.Create(totalAmount, escrows[0].Currency);
        if (payoutMoney.IsFailure)
            return payoutMoney.Error;

        var transaction = Transaction.Create(
            order.SellerId,
            txNumber.Value,
            TransactionType.Payout,
            payoutMoney.Value,
            escrows[0].Currency,
            $"Escrow release for order {order.OrderNumber.Value}. Reason: {reason}",
            _clock.UtcNow,
            order.Id);

        if (transaction.IsFailure)
            return transaction.Error;

        transaction.Value.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
        _dbContext.Insert(transaction.Value);

        var creditResult = sellerWallet.Credit(
            totalAmount,
            transaction.Value.Id,
            $"Escrow release for order {order.OrderNumber.Value}",
            _clock.UtcNow);

        if (creditResult.IsFailure)
            return creditResult.Error;

        foreach (var escrow in escrows)
        {
            var releaseResult = escrow.ReleaseToSeller(
                transaction.Value.Id,
                actorId ?? SystemActorId,
                _clock.UtcNow);

            if (releaseResult.IsFailure)
                return releaseResult.Error;
        }

        var completeResult = order.Complete(_clock.UtcNow);
        if (completeResult.IsFailure)
            return completeResult.Error;

        return UnitResult.Success<Error>();
    }

    public async Task<UnitResult<Error>> RefundBuyerAsync(
        Order order,
        decimal? partialAmount,
        string reason,
        UserId? actorId,
        CancellationToken cancellationToken)
    {
        var escrows = await _dbContext.Set<Escrow>()
            .Where(x => x.OrderId == order.Id && x.Status == EscrowStatus.Holding)
            .ToListAsync(cancellationToken);

        if (escrows.Count == 0)
            return OrderErrors.Order.EscrowNotFound(order.Id);

        var totalHeldAmount = escrows.Sum(x => x.Amount.Amount);
        var refundAmount = partialAmount ?? totalHeldAmount;
        if (refundAmount <= 0 || refundAmount > totalHeldAmount)
            return Error.Validation("Amount", "Refund.InvalidAmount", "Refund amount is out of range.");

        var buyerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.UserId == order.BuyerId && x.IsActive, cancellationToken);

        if (buyerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Buyer wallet not found.");

        var txNumber = TransactionNumber.Create($"REFUND-{Guid.CreateVersion7():N}");
        if (txNumber.IsFailure)
            return txNumber.Error;

        var refundMoney = Money.Create(refundAmount, escrows[0].Currency);
        if (refundMoney.IsFailure)
            return refundMoney.Error;

        var refundTx = Transaction.Create(
            order.BuyerId,
            txNumber.Value,
            TransactionType.Refund,
            refundMoney.Value,
            escrows[0].Currency,
            $"Escrow refund for order {order.OrderNumber.Value}. Reason: {reason}",
            _clock.UtcNow,
            order.Id);

        if (refundTx.IsFailure)
            return refundTx.Error;

        refundTx.Value.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
        _dbContext.Insert(refundTx.Value);

        foreach (var escrow in escrows)
        {
            var refundResult = escrow.RefundToBuyer(
                refundTx.Value.Id,
                actorId ?? SystemActorId,
                _clock.UtcNow);

            if (refundResult.IsFailure)
                return refundResult.Error;
        }

        var creditResult = buyerWallet.Credit(
            refundAmount,
            refundTx.Value.Id,
            $"Escrow refund for order {order.OrderNumber.Value}",
            _clock.UtcNow);

        if (creditResult.IsFailure)
            return creditResult.Error;

        if (partialAmount.HasValue && refundAmount < totalHeldAmount)
        {
            var sellerWallet = await _dbContext.Set<Wallet>()
                .FirstOrDefaultAsync(x => x.UserId == order.SellerId && x.IsActive, cancellationToken);

            if (sellerWallet is not null)
            {
                var remainingAmount = totalHeldAmount - refundAmount;
                var sellerTxNumber = TransactionNumber.Create($"PAYOUT-PART-{Guid.CreateVersion7():N}");
                if (sellerTxNumber.IsFailure)
                    return sellerTxNumber.Error;

                var sellerMoney = Money.Create(remainingAmount, escrows[0].Currency);
                if (sellerMoney.IsFailure)
                    return sellerMoney.Error;

                var payoutTx = Transaction.Create(
                    order.SellerId,
                    sellerTxNumber.Value,
                    TransactionType.Payout,
                    sellerMoney.Value,
                    escrows[0].Currency,
                    $"Partial payout after refund for order {order.OrderNumber.Value}",
                    _clock.UtcNow,
                    order.Id);

                if (payoutTx.IsFailure)
                    return payoutTx.Error;

                payoutTx.Value.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
                _dbContext.Insert(payoutTx.Value);

                var sellerCredit = sellerWallet.Credit(
                    remainingAmount,
                    payoutTx.Value.Id,
                    $"Partial payout after refund for order {order.OrderNumber.Value}",
                    _clock.UtcNow);

                if (sellerCredit.IsFailure)
                    return sellerCredit.Error;
            }
        }

        var markRefundedResult = order.MarkAsRefunded(_clock.UtcNow);
        if (markRefundedResult.IsFailure)
            return markRefundedResult.Error;

        return UnitResult.Success<Error>();
    }
}
