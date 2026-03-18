using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Refunds;

/// <summary>
/// Hoàn tiền (Full hoặc Partial) từ Escrow → Wallet Buyer.
/// Khi Dispute/Return được chấp nhận.
/// </summary>
public sealed record RefundFromEscrowCommand(
    Guid EscrowId,
    decimal? PartialAmount = null) : ICommand;

internal sealed class RefundFromEscrowCommandHandler
    : ICommandHandler<RefundFromEscrowCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public RefundFromEscrowCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(
        RefundFromEscrowCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var actorId = _currentUser.UserId;
        var escrowId = EscrowId.From(request.EscrowId);

        // 1. Tìm Escrow
        var escrow = await _dbContext.Set<Escrow>()
            .Include(e => e.Order)
            .FirstOrDefaultAsync(e => e.Id == escrowId, cancellationToken);

        if (escrow is null)
            return Error.NotFound("Escrow.NotFound", $"Escrow {request.EscrowId} not found.");

        // 2. Xác định số tiền hoàn
        var refundAmount = request.PartialAmount ?? escrow.Amount.Amount;

        if (refundAmount <= 0 || refundAmount > escrow.Amount.Amount)
            return Error.Validation("Amount", "Refund.InvalidAmount",
                $"Refund amount must be between 0 and {escrow.Amount.Amount}.");

        // 3. Tìm Buyer Wallet
        var buyerId = escrow.Order.BuyerId;
        var buyerWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == buyerId && w.IsActive, cancellationToken);

        if (buyerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Buyer wallet not found.");

        // 4. Tạo Refund Transaction
        var txNumberResult = TransactionNumber.Create($"REFUND-{Guid.CreateVersion7():N}");
        if (txNumberResult.IsFailure)
            return txNumberResult.Error;

        var moneyResult = Money.Create(refundAmount, escrow.Currency);
        if (moneyResult.IsFailure)
            return moneyResult.Error;

        var txResult = Transaction.Create(
            buyerId,
            txNumberResult.Value,
            TransactionType.Refund,
            moneyResult.Value,
            escrow.Currency,
            $"Refund for Order #{escrow.OrderId.Value} - " +
            (request.PartialAmount.HasValue ? $"Partial: {refundAmount}" : "Full refund"),
            now,
            escrow.OrderId);

        if (txResult.IsFailure)
            return txResult.Error;

        var transaction = txResult.Value;
        transaction.MarkAsCompleted(GatewayInfo.Empty, now);
        _dbContext.Set<Transaction>().Add(transaction);

        // 5. Refund Escrow → Buyer
        var refundResult = escrow.RefundToBuyer(transaction.Id, actorId, now);
        if (refundResult.IsFailure)
            return refundResult.Error;

        // 6. Credit tiền refund vào Buyer Wallet
        var creditResult = buyerWallet.Credit(
            refundAmount,
            transaction.Id,
            $"Refund from Escrow #{escrow.Id.Value}",
            now);

        if (creditResult.IsFailure)
            return creditResult.Error;

        // 7. Nếu partial refund, credit phần còn lại vào Seller
        if (request.PartialAmount.HasValue && refundAmount < escrow.Amount.Amount)
        {
            var remainingAmount = escrow.Amount.Amount - refundAmount;
            var sellerId = escrow.Order.SellerId;
            var sellerWallet = await _dbContext.Set<Wallet>()
                .FirstOrDefaultAsync(w => w.UserId == sellerId && w.IsActive, cancellationToken);

            if (sellerWallet is not null)
            {
                var sellerTxNumberResult = TransactionNumber.Create($"PAYOUT-PART-{Guid.CreateVersion7():N}");
                if (sellerTxNumberResult.IsFailure) return sellerTxNumberResult.Error;

                var sellerMoneyResult = Money.Create(remainingAmount, escrow.Currency);
                if (sellerMoneyResult.IsFailure) return sellerMoneyResult.Error;

                var sellerTxResult = Transaction.Create(
                    sellerId,
                    sellerTxNumberResult.Value,
                    TransactionType.Payout,
                    sellerMoneyResult.Value,
                    escrow.Currency,
                    $"Partial payout (after partial refund) for Order #{escrow.OrderId.Value}",
                    now,
                    escrow.OrderId);

                if (sellerTxResult.IsSuccess)
                {
                    var sellerTx = sellerTxResult.Value;
                    sellerTx.MarkAsCompleted(GatewayInfo.Empty, now);
                    _dbContext.Set<Transaction>().Add(sellerTx);

                    sellerWallet.Credit(
                        remainingAmount,
                        sellerTx.Id,
                        $"Partial payout from Escrow #{escrow.Id.Value}",
                        now);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
