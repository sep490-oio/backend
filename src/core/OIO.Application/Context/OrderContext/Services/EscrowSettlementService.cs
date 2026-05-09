using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

public sealed record SettlementBreakdown(
    decimal GrossAmount,
    decimal PlatformCommission,
    decimal SellerPayoutBeforeInspection,
    decimal InspectionFee,
    decimal SellerNetAmount,
    decimal PlatformTotal,
    string Currency);

public sealed record RefundSettlementResult(
    decimal RefundAmount,
    decimal SellerRemainingGrossAmount,
    SettlementBreakdown? SellerSettlement);

public sealed record SellerFeeChargeResult(
    decimal FeeAmount,
    bool Collected,
    bool Pending,
    string Currency);

public sealed class EscrowSettlementService
{
    private static readonly UserId SystemActorId = UserId.From(Guid.Empty);

    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IRuntimeSettings _runtimeSettings;

    public EscrowSettlementService(
        IDbContext dbContext,
        IClock clock,
        IRuntimeSettings runtimeSettings)
    {
        _dbContext = dbContext;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<SettlementBreakdown, Error>> ReleaseToSellerAsync(
        Order order,
        string reason,
        UserId? actorId,
        CancellationToken cancellationToken)
    {
        var escrows = await LoadHoldingEscrowsAsync(order, cancellationToken);
        if (escrows.Count == 0)
            return OrderErrors.Order.EscrowNotFound(order.Id);

        var currency = escrows[0].Currency;
        var sellerWallet = await GetActiveWalletAsync(order.SellerId, currency, cancellationToken);
        if (sellerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        // Fee calculation uses the canonical order total — NOT escrows.Sum() —
        // because legacy escrow data may be inconsistent (missing deposit escrow
        // or double-counted wallet hold). The order's TotalAmount is the single
        // source of truth for the auction final price.
        var grossAmount = order.Pricing.TotalAmount.Amount;
        var settlementResult = CalculateSellerSettlement(
            grossAmount,
            currency,
            order.IsPlatformVerifiedItem);
        if (settlementResult.IsFailure)
            return settlementResult.Error;

        var settlement = settlementResult.Value;
        var payoutTxResult = await CreateCompletedTransactionAsync(
            transactionNumber: $"PAYOUT-{order.Id.Value:N}",
            userId: order.SellerId,
            type: TransactionType.Payout,
            grossAmount: settlement.GrossAmount,
            currency: currency,
            description: $"Escrow release for order {order.OrderNumber.Value}. Reason: {reason}",
            order: order,
            fee: settlement.PlatformCommission,
            netAmount: settlement.SellerPayoutBeforeInspection,
            cancellationToken: cancellationToken);
        if (payoutTxResult.IsFailure)
            return payoutTxResult.Error;

        var payoutTx = payoutTxResult.Value;
        var sellerCreditResult = await EnsureWalletCreditAsync(
            sellerWallet,
            settlement.SellerPayoutBeforeInspection,
            payoutTx.Id,
            $"Escrow release for order {order.OrderNumber.Value}",
            cancellationToken);
        if (sellerCreditResult.IsFailure)
            return sellerCreditResult.Error;

        var platformCommissionResult = await CreditPlatformWalletAsync(
            platformWalletResult.Value,
            settlement.PlatformCommission,
            payoutTx.Id,
            $"Platform commission for order {order.OrderNumber.Value}",
            cancellationToken);
        if (platformCommissionResult.IsFailure)
            return platformCommissionResult.Error;

        if (settlement.InspectionFee > 0m)
        {
            var feeResult = await ApplyInspectionFeeChargeAsync(
                order,
                sellerWallet,
                platformWalletResult.Value,
                settlement.InspectionFee,
                transactionNumber: $"FEE-INSP-{order.Id.Value:N}",
                description: $"Offline inspection fee for order {order.OrderNumber.Value}",
                allowPendingWhenInsufficient: false,
                cancellationToken: cancellationToken);
            if (feeResult.IsFailure)
                return feeResult.Error;
        }

        foreach (var escrow in escrows)
        {
            var releaseResult = escrow.ReleaseToSeller(
                payoutTx.Id,
                actorId ?? SystemActorId,
                _clock.UtcNow);

            if (releaseResult.IsFailure)
                return releaseResult.Error;
        }

        var completeResult = order.Complete(_clock.UtcNow);
        if (completeResult.IsFailure)
            return completeResult.Error;

        return settlement;
    }

    public async Task<Result<RefundSettlementResult, Error>> RefundBuyerAsync(
        Order order,
        decimal? partialAmount,
        string reason,
        UserId? actorId,
        CancellationToken cancellationToken)
    {
        var escrows = await LoadHoldingEscrowsAsync(order, cancellationToken);
        if (escrows.Count == 0)
            return OrderErrors.Order.EscrowNotFound(order.Id);

        var currency = escrows[0].Currency;
        var totalHeldAmount = escrows.Sum(x => x.Amount.Amount);
        var refundAmount = partialAmount ?? totalHeldAmount;
        if (refundAmount <= 0 || refundAmount > totalHeldAmount)
            return Error.Validation("Amount", "Refund.InvalidAmount", "Refund amount is out of range.");

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        var buyerWallet = await GetActiveWalletAsync(order.BuyerId, currency, cancellationToken);
        if (buyerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Buyer wallet not found.");

        var refundTxResult = await CreateCompletedTransactionAsync(
            transactionNumber: partialAmount.HasValue
                ? $"REFUND-PART-{order.Id.Value:N}"
                : $"REFUND-{order.Id.Value:N}",
            userId: order.BuyerId,
            type: TransactionType.Refund,
            grossAmount: refundAmount,
            currency: currency,
            description: $"Escrow refund for order {order.OrderNumber.Value}. Reason: {reason}",
            order: order,
            fee: 0m,
            netAmount: refundAmount,
            cancellationToken: cancellationToken);
        if (refundTxResult.IsFailure)
            return refundTxResult.Error;

        var refundTx = refundTxResult.Value;
        foreach (var escrow in escrows)
        {
            var refundResult = escrow.RefundToBuyer(
                refundTx.Id,
                actorId ?? SystemActorId,
                _clock.UtcNow);

            if (refundResult.IsFailure)
                return refundResult.Error;
        }

        var buyerCreditResult = await EnsureWalletCreditAsync(
            buyerWallet,
            refundAmount,
            refundTx.Id,
            $"Escrow refund for order {order.OrderNumber.Value}",
            cancellationToken);
        if (buyerCreditResult.IsFailure)
            return buyerCreditResult.Error;

        SettlementBreakdown? sellerSettlement = null;
        var remainingAmount = totalHeldAmount - refundAmount;
        if (partialAmount.HasValue && remainingAmount > 0m)
        {
            var sellerSettlementResult = await SettleRemainingAmountToSellerAsync(
                order,
                remainingAmount,
                currency,
                cancellationToken);
            if (sellerSettlementResult.IsFailure)
                return sellerSettlementResult.Error;

            sellerSettlement = sellerSettlementResult.Value;
        }

        var markRefundedResult = order.MarkAsRefunded(_clock.UtcNow);
        if (markRefundedResult.IsFailure)
            return markRefundedResult.Error;

        return new RefundSettlementResult(refundAmount, remainingAmount, sellerSettlement);
    }

    /// <summary>
    /// Charges the seller's wallet the platform commission fee after a buyer-win
    /// dispute resolution that triggers a full refund. The buyer receives the entire
    /// escrow amount; this method separately debits the seller's wallet for the
    /// platform's cut. If the seller's wallet has insufficient funds, the charge
    /// is recorded as pending.
    /// </summary>
    public async Task<Result<SellerFeeChargeResult, Error>> ChargeSellerCommissionOnBuyerRefundAsync(
        Order order,
        Guid disputeId,
        string reason,
        CancellationToken cancellationToken)
    {
        var currency = order.Currency;
        var grossAmount = order.Pricing.TotalAmount.Amount;

        var settlementResult = CalculateSellerSettlement(
            grossAmount,
            currency,
            includeInspectionFee: false);
        if (settlementResult.IsFailure)
            return settlementResult.Error;

        var commissionAmount = settlementResult.Value.PlatformCommission;
        if (commissionAmount <= 0m)
            return new SellerFeeChargeResult(0m, Collected: false, Pending: false, currency);

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        var sellerWallet = await GetActiveWalletAsync(order.SellerId, currency, cancellationToken);

        var txNumber = $"FEE-COMM-DSP-{disputeId:N}";
        var existingTx = await FindTransactionAsync(txNumber, cancellationToken);
        if (existingTx is not null && existingTx.Status == TransactionStatus.Completed)
            return new SellerFeeChargeResult(commissionAmount, Collected: true, Pending: false, currency);

        var description = $"Platform commission charged to seller for buyer-win dispute on order {order.OrderNumber.Value}. Reason: {reason}";

        if (sellerWallet is null || sellerWallet.WalletFunds.BalanceAmount < commissionAmount)
        {
            // Insufficient funds - record pending transaction for later collection
            if (existingTx is null)
            {
                var pendingTxResult = await CreateTransactionAsync(
                    transactionNumber: txNumber,
                    userId: order.SellerId,
                    type: TransactionType.Fee,
                    grossAmount: commissionAmount,
                    currency: currency,
                    description: description,
                    order: order,
                    fee: 0m,
                    netAmount: commissionAmount,
                    cancellationToken: cancellationToken);
                if (pendingTxResult.IsFailure)
                    return pendingTxResult.Error;
            }

            return new SellerFeeChargeResult(commissionAmount, Collected: false, Pending: true, currency);
        }

        // Debit seller, credit platform
        var feeTxResult = await CreateCompletedTransactionAsync(
            transactionNumber: txNumber,
            userId: order.SellerId,
            type: TransactionType.Fee,
            grossAmount: commissionAmount,
            currency: currency,
            description: description,
            order: order,
            fee: 0m,
            netAmount: commissionAmount,
            cancellationToken: cancellationToken);
        if (feeTxResult.IsFailure)
            return feeTxResult.Error;

        var debitResult = await EnsureWalletDebitAsync(
            sellerWallet,
            commissionAmount,
            feeTxResult.Value.Id,
            description,
            cancellationToken);
        if (debitResult.IsFailure)
            return debitResult.Error;

        var creditResult = await CreditPlatformWalletAsync(
            platformWalletResult.Value,
            commissionAmount,
            feeTxResult.Value.Id,
            $"Platform commission from seller for buyer-win dispute on order {order.OrderNumber.Value}",
            cancellationToken);
        if (creditResult.IsFailure)
            return creditResult.Error;

        return new SellerFeeChargeResult(commissionAmount, Collected: true, Pending: false, currency);
    }

    public async Task<Result<SellerFeeChargeResult, Error>> ChargeVerifiedInspectionFeeForBuyerWinAsync(
        Order order,
        Guid disputeId,
        string reason,
        UserId? actorId,
        CancellationToken cancellationToken)
    {
        if (!order.IsPlatformVerifiedItem)
            return new SellerFeeChargeResult(0m, Collected: false, Pending: false, order.Currency);

        var escrows = await _dbContext.Set<Escrow>()
            .Where(x => x.OrderId == order.Id)
            .ToListAsync(cancellationToken);

        if (escrows.Count == 0)
            return OrderErrors.Order.EscrowNotFound(order.Id);

        var currency = escrows[0].Currency;
        var grossAmount = escrows.Sum(x => x.Amount.Amount);
        var settlementResult = CalculateSellerSettlement(
            grossAmount,
            currency,
            includeInspectionFee: true);
        if (settlementResult.IsFailure)
            return settlementResult.Error;

        var feeAmount = settlementResult.Value.InspectionFee;
        if (feeAmount <= 0m)
            return new SellerFeeChargeResult(0m, Collected: false, Pending: false, currency);

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        var sellerWallet = await GetActiveWalletAsync(order.SellerId, currency, cancellationToken);
        var feeResult = await ApplyInspectionFeeChargeAsync(
            order,
            sellerWallet,
            platformWalletResult.Value,
            feeAmount,
            transactionNumber: $"FEE-INSP-DSP-{disputeId:N}",
            description: $"Buyer-win dispute inspection fee for order {order.OrderNumber.Value}. Reason: {reason}",
            allowPendingWhenInsufficient: true,
            cancellationToken: cancellationToken);
        if (feeResult.IsFailure)
            return feeResult.Error;

        return feeResult.Value;
    }

    private async Task<Result<SettlementBreakdown, Error>> SettleRemainingAmountToSellerAsync(
        Order order,
        decimal remainingAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        var sellerWallet = await GetActiveWalletAsync(order.SellerId, currency, cancellationToken);
        if (sellerWallet is null)
            return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        var settlementResult = CalculateSellerSettlement(
            remainingAmount,
            currency,
            order.IsPlatformVerifiedItem);
        if (settlementResult.IsFailure)
            return settlementResult.Error;

        var settlement = settlementResult.Value;
        var payoutTxResult = await CreateCompletedTransactionAsync(
            transactionNumber: $"PAYOUT-PART-{order.Id.Value:N}",
            userId: order.SellerId,
            type: TransactionType.Payout,
            grossAmount: settlement.GrossAmount,
            currency: currency,
            description: $"Partial payout after refund for order {order.OrderNumber.Value}",
            order: order,
            fee: settlement.PlatformCommission,
            netAmount: settlement.SellerPayoutBeforeInspection,
            cancellationToken: cancellationToken);
        if (payoutTxResult.IsFailure)
            return payoutTxResult.Error;

        var payoutTx = payoutTxResult.Value;
        var sellerCreditResult = await EnsureWalletCreditAsync(
            sellerWallet,
            settlement.SellerPayoutBeforeInspection,
            payoutTx.Id,
            $"Partial payout after refund for order {order.OrderNumber.Value}",
            cancellationToken);
        if (sellerCreditResult.IsFailure)
            return sellerCreditResult.Error;

        var platformCommissionResult = await CreditPlatformWalletAsync(
            platformWalletResult.Value,
            settlement.PlatformCommission,
            payoutTx.Id,
            $"Platform commission for partial payout of order {order.OrderNumber.Value}",
            cancellationToken);
        if (platformCommissionResult.IsFailure)
            return platformCommissionResult.Error;

        if (settlement.InspectionFee > 0m)
        {
            var feeResult = await ApplyInspectionFeeChargeAsync(
                order,
                sellerWallet,
                platformWalletResult.Value,
                settlement.InspectionFee,
                transactionNumber: $"FEE-INSP-PART-{order.Id.Value:N}",
                description: $"Offline inspection fee for partial payout of order {order.OrderNumber.Value}",
                allowPendingWhenInsufficient: false,
                cancellationToken: cancellationToken);
            if (feeResult.IsFailure)
                return feeResult.Error;
        }

        return settlement;
    }

    private Result<SettlementBreakdown, Error> CalculateSellerSettlement(
        decimal grossAmount,
        string currency,
        bool includeInspectionFee) =>
        CalculateSellerSettlement(_runtimeSettings.Settlement, grossAmount, currency, includeInspectionFee);

    /// <summary>
    /// Pure fee calculator. Mirrors the logic invoked by the
    /// <c>EscrowSettlementService</c> when releasing escrows so read-side
    /// queries (e.g. seller finance overview / ledger) can preview the same
    /// breakdown without re-implementing fee math. Reads rates and caps from
    /// the supplied <see cref="SettlementOptions"/> — never hard-coded.
    /// </summary>
    public static Result<SettlementBreakdown, Error> CalculateSellerSettlement(
        SettlementOptions options,
        decimal grossAmount,
        string currency,
        bool includeInspectionFee)
    {
        if (grossAmount <= 0m)
            return Error.Validation("Amount", "Settlement.InvalidGrossAmount", "Settlement gross amount must be positive.");

        if (!options.TryGetInspectionFeeCap(currency, out var inspectionFeeCap))
        {
            return Error.Validation(
                "Settlement.OfflineInspectionFeeCapsByCurrency",
                "Settlement.MissingInspectionFeeCap",
                $"Missing offline inspection fee cap for currency '{currency}'.");
        }

        var scale = options.GetDecimalPlaces(currency);
        var platformCommission = RoundMoney(grossAmount * options.SellerCommissionRate, scale);
        var sellerPayoutBeforeInspection = RoundMoney(grossAmount - platformCommission, scale);
        var inspectionFee = includeInspectionFee
            ? RoundMoney(Math.Min(sellerPayoutBeforeInspection * options.OfflineInspectionFeeRate, inspectionFeeCap), scale)
            : 0m;
        var sellerNetAmount = RoundMoney(sellerPayoutBeforeInspection - inspectionFee, scale);
        var platformTotal = RoundMoney(platformCommission + inspectionFee, scale);

        if (sellerNetAmount < 0m)
            return Error.Validation("Settlement", "Settlement.InvalidNetAmount", "Settlement net seller amount cannot be negative.");

        return new SettlementBreakdown(
            GrossAmount: grossAmount,
            PlatformCommission: platformCommission,
            SellerPayoutBeforeInspection: sellerPayoutBeforeInspection,
            InspectionFee: inspectionFee,
            SellerNetAmount: sellerNetAmount,
            PlatformTotal: platformTotal,
            Currency: currency);
    }

    private async Task<Result<SellerFeeChargeResult, Error>> ApplyInspectionFeeChargeAsync(
        Order order,
        Wallet? sellerWallet,
        Wallet platformWallet,
        decimal feeAmount,
        string transactionNumber,
        string description,
        bool allowPendingWhenInsufficient,
        CancellationToken cancellationToken)
    {
        if (feeAmount <= 0m)
            return new SellerFeeChargeResult(0m, Collected: false, Pending: false, order.Currency);

        var existingTx = await FindTransactionAsync(transactionNumber, cancellationToken);
        if (existingTx is not null && existingTx.Status == TransactionStatus.Completed)
        {
            return new SellerFeeChargeResult(
                feeAmount,
                Collected: true,
                Pending: false,
                order.Currency);
        }

        Transaction feeTx;
        if (existingTx is not null)
        {
            feeTx = existingTx;
        }
        else
        {
            var feeTxResult = await CreateTransactionAsync(
                transactionNumber: transactionNumber,
                userId: order.SellerId,
                type: TransactionType.Fee,
                grossAmount: feeAmount,
                currency: order.Currency,
                description: description,
                order: order,
                fee: 0m,
                netAmount: feeAmount,
                cancellationToken: cancellationToken);
            if (feeTxResult.IsFailure)
                return feeTxResult.Error;

            feeTx = feeTxResult.Value;
        }

        if (sellerWallet is null)
        {
            if (!allowPendingWhenInsufficient)
                return Error.NotFound("Wallet.NotFound", "Seller wallet not found.");

            return new SellerFeeChargeResult(feeAmount, Collected: false, Pending: true, order.Currency);
        }

        var debitResult = await EnsureWalletDebitAsync(
            sellerWallet,
            feeAmount,
            feeTx.Id,
            description,
            cancellationToken);
        if (debitResult.IsFailure)
        {
            if (!allowPendingWhenInsufficient)
                return debitResult.Error;

            return new SellerFeeChargeResult(feeAmount, Collected: false, Pending: true, order.Currency);
        }

        if (feeTx.Status != TransactionStatus.Completed)
        {
            var completeResult = feeTx.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
            if (completeResult.IsFailure)
                return completeResult.Error;
        }

        var platformCreditResult = await CreditPlatformWalletAsync(
            platformWallet,
            feeAmount,
            feeTx.Id,
            description,
            cancellationToken);
        if (platformCreditResult.IsFailure)
            return platformCreditResult.Error;

        return new SellerFeeChargeResult(feeAmount, Collected: true, Pending: false, order.Currency);
    }

    private async Task<List<Escrow>> LoadHoldingEscrowsAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Escrow>()
            .Where(x => x.OrderId == order.Id && x.Status == EscrowStatus.Holding)
            .ToListAsync(cancellationToken);
    }

    private async Task<Wallet?> GetActiveWalletAsync(
        UserId userId,
        string currency,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                     && x.IsActive
                     && x.WalletFunds.Currency.Id == currency,
                cancellationToken);
    }

    private async Task<Result<Wallet, Error>> GetPlatformWalletAsync(
        string currency,
        CancellationToken cancellationToken)
    {
        var platformWallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(
                x => x.Type == WalletType.Platform
                     && x.IsActive
                     && x.WalletFunds.Currency.Id == currency,
                cancellationToken);

        if (platformWallet is null)
            return Error.NotFound("PlatformWallet.NotFound", $"Platform wallet for currency '{currency}' not found.");

        return platformWallet;
    }

    private async Task<UnitResult<Error>> CreditPlatformWalletAsync(
        Wallet platformWallet,
        decimal amount,
        TransactionId transactionId,
        string description,
        CancellationToken cancellationToken)
    {
        if (amount <= 0m)
            return UnitResult.Success<Error>();

        return await EnsureWalletCreditAsync(
            platformWallet,
            amount,
            transactionId,
            description,
            cancellationToken);
    }

    private async Task<UnitResult<Error>> EnsureWalletCreditAsync(
        Wallet wallet,
        decimal amount,
        TransactionId transactionId,
        string description,
        CancellationToken cancellationToken)
    {
        if (await HasWalletTransactionAsync(wallet.Id, transactionId, cancellationToken))
            return UnitResult.Success<Error>();

        return wallet.Credit(amount, transactionId, description, _clock.UtcNow);
    }

    private async Task<UnitResult<Error>> EnsureWalletDebitAsync(
        Wallet wallet,
        decimal amount,
        TransactionId transactionId,
        string description,
        CancellationToken cancellationToken)
    {
        if (await HasWalletTransactionAsync(wallet.Id, transactionId, cancellationToken))
            return UnitResult.Success<Error>();

        return wallet.Debit(amount, transactionId, description, _clock.UtcNow);
    }

    private async Task<bool> HasWalletTransactionAsync(
        WalletId walletId,
        TransactionId transactionId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<WalletTransaction>()
            .AnyAsync(
                x => x.WalletId == walletId && x.TransactionId == transactionId,
                cancellationToken);
    }

    private async Task<Result<Transaction, Error>> CreateCompletedTransactionAsync(
        string transactionNumber,
        UserId userId,
        TransactionType type,
        decimal grossAmount,
        string currency,
        string description,
        Order order,
        decimal fee,
        decimal netAmount,
        CancellationToken cancellationToken)
    {
        var transactionResult = await CreateTransactionAsync(
            transactionNumber,
            userId,
            type,
            grossAmount,
            currency,
            description,
            order,
            fee,
            netAmount,
            cancellationToken);
        if (transactionResult.IsFailure)
            return transactionResult.Error;

        if (transactionResult.Value.Status != TransactionStatus.Completed)
        {
            var completeResult = transactionResult.Value.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
            if (completeResult.IsFailure)
                return completeResult.Error;
        }

        return transactionResult.Value;
    }

    private async Task<Result<Transaction, Error>> CreateTransactionAsync(
        string transactionNumber,
        UserId userId,
        TransactionType type,
        decimal grossAmount,
        string currency,
        string description,
        Order order,
        decimal fee,
        decimal netAmount,
        CancellationToken cancellationToken)
    {
        var existingTx = await FindTransactionAsync(transactionNumber, cancellationToken);
        if (existingTx is not null)
            return existingTx;

        var txNumber = TransactionNumber.Create(transactionNumber);
        if (txNumber.IsFailure)
            return txNumber.Error;

        var grossMoney = Money.Create(grossAmount, currency);
        if (grossMoney.IsFailure)
            return grossMoney.Error;

        var netMoney = Money.Create(netAmount, currency);
        if (netMoney.IsFailure)
            return netMoney.Error;

        var transaction = Transaction.Create(
            userId,
            txNumber.Value,
            type,
            grossMoney.Value,
            currency,
            description,
            _clock.UtcNow,
            order.Id);
        if (transaction.IsFailure)
            return transaction.Error;

        var feeResult = transaction.Value.SetFee(fee, netMoney.Value);
        if (feeResult.IsFailure)
            return feeResult.Error;

        _dbContext.Insert(transaction.Value);
        return transaction.Value;
    }

    /// <summary>
    /// Charges the flat inspection fee (fee cap) to the seller when an inspector
    /// rejects their item during the warehouse verification flow. Because there is
    /// no order at this point, the transaction is created without an OrderId.
    /// If the seller has insufficient wallet balance the charge is recorded as
    /// pending for later collection.
    /// </summary>
    public async Task<Result<SellerFeeChargeResult, Error>> ChargeInspectionFeeOnRejectionAsync(
        UserId sellerId,
        Guid inspectionId,
        string currency,
        string itemTitle,
        CancellationToken cancellationToken)
    {
        var options = _runtimeSettings.Settlement;
        if (!options.TryGetInspectionFeeCap(currency, out var feeAmount))
        {
            return Error.Validation(
                "Settlement.OfflineInspectionFeeCapsByCurrency",
                "Settlement.MissingInspectionFeeCap",
                $"Missing offline inspection fee cap for currency '{currency}'.");
        }

        if (feeAmount <= 0m)
            return new SellerFeeChargeResult(0m, Collected: false, Pending: false, currency);

        var transactionNumber = $"FEE-INSP-REJ-{inspectionId:N}";
        var description = $"Inspection fee for rejected item \"{itemTitle}\" (inspection {inspectionId}).";

        // Idempotency check — if this fee was already charged, skip.
        var existingTx = await FindTransactionAsync(transactionNumber, cancellationToken);
        if (existingTx is not null && existingTx.Status == TransactionStatus.Completed)
            return new SellerFeeChargeResult(feeAmount, Collected: true, Pending: false, currency);

        var platformWalletResult = await GetPlatformWalletAsync(currency, cancellationToken);
        if (platformWalletResult.IsFailure)
            return platformWalletResult.Error;

        var sellerWallet = await GetActiveWalletAsync(sellerId, currency, cancellationToken);

        // Create the fee transaction (no order link).
        Transaction feeTx;
        if (existingTx is not null)
        {
            feeTx = existingTx;
        }
        else
        {
            var txNumber = TransactionNumber.Create(transactionNumber);
            if (txNumber.IsFailure)
                return txNumber.Error;

            var grossMoney = Money.Create(feeAmount, currency);
            if (grossMoney.IsFailure)
                return grossMoney.Error;

            var netMoney = Money.Create(feeAmount, currency);
            if (netMoney.IsFailure)
                return netMoney.Error;

            var txResult = Transaction.Create(
                sellerId,
                txNumber.Value,
                TransactionType.Fee,
                grossMoney.Value,
                currency,
                description,
                _clock.UtcNow);
            if (txResult.IsFailure)
                return txResult.Error;

            var feeResult = txResult.Value.SetFee(0m, netMoney.Value);
            if (feeResult.IsFailure)
                return feeResult.Error;

            feeTx = txResult.Value;
            _dbContext.Insert(feeTx);
        }

        // Debit seller wallet.
        if (sellerWallet is null)
            return new SellerFeeChargeResult(feeAmount, Collected: false, Pending: true, currency);

        var debitResult = await EnsureWalletDebitAsync(
            sellerWallet,
            feeAmount,
            feeTx.Id,
            description,
            cancellationToken);
        if (debitResult.IsFailure)
            return new SellerFeeChargeResult(feeAmount, Collected: false, Pending: true, currency);

        if (feeTx.Status != TransactionStatus.Completed)
        {
            var completeResult = feeTx.MarkAsCompleted(GatewayInfo.Empty, _clock.UtcNow);
            if (completeResult.IsFailure)
                return completeResult.Error;
        }

        // Credit platform wallet.
        var platformCreditResult = await CreditPlatformWalletAsync(
            platformWalletResult.Value,
            feeAmount,
            feeTx.Id,
            description,
            cancellationToken);
        if (platformCreditResult.IsFailure)
            return platformCreditResult.Error;

        return new SellerFeeChargeResult(feeAmount, Collected: true, Pending: false, currency);
    }

    private async Task<Transaction?> FindTransactionAsync(
        string transactionNumber,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Transaction>()
            .FirstOrDefaultAsync(x => x.TransactionNumber.Value == transactionNumber, cancellationToken);
    }

    private static decimal RoundMoney(decimal value, int decimalPlaces) =>
        Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero);
}
