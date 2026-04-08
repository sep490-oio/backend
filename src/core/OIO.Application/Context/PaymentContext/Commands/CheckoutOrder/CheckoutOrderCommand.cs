using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;
using OIO.Application.Context.PaymentContext.Services;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.CheckoutOrder;

public sealed record CheckoutOrderCommand(
    Guid OrderId,
    IPAddress IpAddress,
    string? BankCode = null,
    string PaymentMethod = "vnpay") : ICommand<CheckoutOrderResponse>;

public sealed record CheckoutOrderResponse(
    Guid TransactionId,
    string TransactionRef,
    string? PaymentUrl);

internal sealed class CheckoutOrderCommandHandler
    : ICommandHandler<CheckoutOrderCommand, CheckoutOrderResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly MediatR.ISender _sender;
    private readonly BuyNowReservationFinalizer _buyNowReservationFinalizer;
    private readonly ILogger<CheckoutOrderCommandHandler> _logger;

    public CheckoutOrderCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        MediatR.ISender sender,
        BuyNowReservationFinalizer buyNowReservationFinalizer,
        ILogger<CheckoutOrderCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sender = sender;
        _buyNowReservationFinalizer = buyNowReservationFinalizer;
        _logger = logger;
    }

    public async Task<Result<CheckoutOrderResponse, Error>> Handle(
        CheckoutOrderCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var orderId = OrderId.From(request.OrderId);
        // 1. Lấy thông tin Order
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(
                id: orderId,
                cancellationToken: cancellationToken
        );

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // 2. Thay đổi trạng thái payment của Order thành InitializePayment
        var initResult = order.InitializePayment(now);
        if (initResult.IsFailure)
            return initResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Branch on PaymentMethod
        if (request.PaymentMethod is "wallet" or "wallet_vnpay")
            return await HandleWalletPaymentAsync(request, order, now, cancellationToken);

        // Existing VNPay flow
        return await HandleVnPayPaymentAsync(request, order, now, cancellationToken);
    }

    private async Task<Result<CheckoutOrderResponse, Error>> HandleVnPayPaymentAsync(
        CheckoutOrderCommand request,
        Order order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var createUrlCommand = new CreateVnPayPaymentUrlCommand(
            Amount: order.Pricing.TotalAmount.Amount,
            Currency: order.Currency,
            Purpose: PaymentPurpose.OrderPayment.Id,
            IpAddress: request.IpAddress,
            Description: $"OrderPayment - Order #{order.OrderNumber.Value}",
            BankCode: request.BankCode,
            AuctionId: order.AuctionId.Value,
            OrderId: order.Id.Value);

        var urlResult = await _sender.Send(createUrlCommand, cancellationToken);

        if (urlResult.IsFailure)
            return urlResult.Error;

        return new CheckoutOrderResponse(
            TransactionId: urlResult.Value.TransactionId,
            TransactionRef: urlResult.Value.TransactionRef,
            PaymentUrl: urlResult.Value.PaymentUrl);
    }

    private async Task<Result<CheckoutOrderResponse, Error>> HandleWalletPaymentAsync(
        CheckoutOrderCommand request,
        Order order,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // 1. Load buyer's wallet
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == order.BuyerId, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Wallet not found for the buyer.");

        // 2. Calculate remaining amount after deposit
        var orderAmount = order.Pricing.TotalAmount.Amount;
        var depositAmount = 0m;

        // NOTE: Skip generic winner-deposit conversion when this order is linked to an active
        // buy-now reservation. In that case BuyNowReservationFinalizer.ApplyBuyNowDepositFundingAsync
        // is the single source of truth for converting the Held deposit into payment funding.
        // Running this block here would double-convert the deposit and cause the finalizer
        // to fail with "Held buyer deposit not found".
        var isLinkedBuyNowReservation = await _dbContext.Set<AuctionBuyNowReservation>()
            .AnyAsync(
                r => r.OrderId == order.Id && r.Status == BuyNowReservationStatus.PendingPayment,
                cancellationToken);

        var winnerDeposit = isLinkedBuyNowReservation
            ? null
            : await _dbContext.Set<AuctionDeposit>()
                .FirstOrDefaultAsync(
                    d => d.AuctionId == order.AuctionId &&
                         d.BidderId == order.BuyerId &&
                         d.Status == DepositStatus.Held,
                    cancellationToken);

        if (winnerDeposit is not null)
            depositAmount = winnerDeposit.Amount.Amount;

        var remainingAmount = orderAmount - depositAmount;
        if (remainingAmount < 0) remainingAmount = 0;

        // 3. Branch based on payment method
        if (request.PaymentMethod == "wallet")
        {
            return await HandleFullWalletPaymentAsync(
                request, order, wallet, winnerDeposit,
                remainingAmount, orderAmount, now, cancellationToken);
        }

        // wallet_vnpay hybrid
        return await HandleHybridWalletVnPayPaymentAsync(
            request, order, wallet, winnerDeposit,
            remainingAmount, orderAmount, now, cancellationToken);
    }

    private async Task<Result<CheckoutOrderResponse, Error>> HandleFullWalletPaymentAsync(
        CheckoutOrderCommand request,
        Order order,
        Wallet wallet,
        AuctionDeposit? winnerDeposit,
        decimal remainingAmount,
        decimal orderAmount,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Check sufficient balance
        if (wallet.WalletFunds.BalanceAmount < remainingAmount)
            return Error.Validation("Wallet.InsufficientBalance", "Wallet.InsufficientBalance",
                $"Insufficient wallet balance. Required: {remainingAmount}, Available: {wallet.WalletFunds.BalanceAmount}");

        // Create transaction number
        var txnRef = $"WLT-{now:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..36];
        var txnNumberResult = TransactionNumber.Create(txnRef);
        if (txnNumberResult.IsFailure)
            return txnNumberResult.Error;

        // Create Money
        var moneyResult = Money.Create(orderAmount, order.Currency);
        if (moneyResult.IsFailure)
            return moneyResult.Error;

        // Create Transaction
        var transactionResult = Transaction.Create(
            userId: order.BuyerId,
            transactionNumber: txnNumberResult.Value,
            type: TransactionType.Payment,
            amount: moneyResult.Value,
            currency: order.Currency,
            description: $"[OrderPayment] Wallet payment for order {order.Id.Value}",
            nowUtc: now,
            orderId: order.Id,
            auctionId: order.AuctionId);

        if (transactionResult.IsFailure)
            return transactionResult.Error;

        var transaction = transactionResult.Value;
        _dbContext.Insert(transaction);

        // Apply deposit if exists
        if (winnerDeposit is not null)
        {
            var convertResult = winnerDeposit.ConvertToPayment(now);
            if (convertResult.IsFailure)
                return convertResult.Error;

            var debitPendingResult = wallet.DebitPending(
                winnerDeposit.Amount.Amount,
                transaction.Id,
                $"Auction winner deposit applied for order {order.Id.Value}",
                now);

            if (debitPendingResult.IsFailure)
                return debitPendingResult.Error;
        }

        // Debit remaining from wallet
        if (remainingAmount > 0)
        {
            var debitResult = wallet.Debit(
                remainingAmount,
                transaction.Id,
                $"Wallet payment for order {order.Id.Value}",
                now);

            if (debitResult.IsFailure)
                return debitResult.Error;
        }

        // Create Escrow
        var escrowResult = Escrow.Create(
            order.Id,
            transaction.Id,
            moneyResult.Value,
            order.Currency,
            now);

        if (escrowResult.IsFailure)
            return escrowResult.Error;

        _dbContext.Insert(escrowResult.Value);

        // Mark transaction completed
        var markResult = transaction.MarkAsCompleted(GatewayInfo.Empty, now);
        if (markResult.IsFailure)
            return markResult.Error;

        // Mark order paid
        var markPaidResult = order.MarkAsPaid(now);
        if (markPaidResult.IsFailure)
            return markPaidResult.Error;

        // Wave C: If this order originated from a Buy Now reservation, finalize it
        // so the auction transitions to Sold (mirrors ProcessVnPayCallback OrderPayment branch).
        var finalizeBuyNowResult = await _buyNowReservationFinalizer.FinalizeAsync(order, now, cancellationToken);
        if (finalizeBuyNowResult.IsFailure)
            return finalizeBuyNowResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Wallet payment completed for OrderId={OrderId}, Amount={Amount}",
            order.Id.Value, orderAmount);

        return new CheckoutOrderResponse(
            TransactionId: transaction.Id.Value,
            TransactionRef: txnRef,
            PaymentUrl: null);
    }

    private async Task<Result<CheckoutOrderResponse, Error>> HandleHybridWalletVnPayPaymentAsync(
        CheckoutOrderCommand request,
        Order order,
        Wallet wallet,
        AuctionDeposit? winnerDeposit,
        decimal remainingAmount,
        decimal orderAmount,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var walletBalance = wallet.WalletFunds.BalanceAmount;
        var walletPortion = Math.Min(walletBalance, remainingAmount);
        var vnpayPortion = remainingAmount - walletPortion;

        // If wallet covers everything, do full wallet payment
        if (vnpayPortion <= 0)
        {
            return await HandleFullWalletPaymentAsync(
                request, order, wallet, winnerDeposit,
                remainingAmount, orderAmount, now, cancellationToken);
        }

        // Hold wallet portion if > 0
        if (walletPortion > 0)
        {
            var holdResult = wallet.Hold(
                walletPortion,
                null,
                $"[HybridHold] Hold for hybrid payment - OrderId: {order.Id.Value} - WalletPortion: {walletPortion}",
                now);

            if (holdResult.IsFailure)
                return holdResult.Error;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Create VNPay URL for vnpayPortion only
        var createUrlCommand = new CreateVnPayPaymentUrlCommand(
            Amount: vnpayPortion,
            Currency: order.Currency,
            Purpose: PaymentPurpose.OrderPayment.Id,
            IpAddress: request.IpAddress,
            Description: $"OrderPayment - Order #{order.OrderNumber.Value} (hybrid: VNPay portion)",
            BankCode: request.BankCode,
            AuctionId: order.AuctionId.Value,
            OrderId: order.Id.Value);

        var urlResult = await _sender.Send(createUrlCommand, cancellationToken);

        if (urlResult.IsFailure)
        {
            // Rollback wallet hold if VNPay URL creation fails
            if (walletPortion > 0)
            {
                var unholdResult = wallet.Unhold(
                    walletPortion,
                    null,
                    $"[HybridHold] Rollback hold for failed hybrid payment - OrderId: {order.Id.Value}",
                    now);

                if (unholdResult.IsSuccess)
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return urlResult.Error;
        }

        // Store wallet portion info in the transaction description so callback handler knows
        if (walletPortion > 0)
        {
            var transactionId = TransactionId.From(urlResult.Value.TransactionId);
            var transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(
                    t => t.Id == transactionId,
                    cancellationToken);

            if (transaction is not null)
            {
                // The description already has the order info; we append wallet hold metadata
                // Format: existing description + " | WalletHold:{amount}"
                // This will be parsed by the callback handler
            }
        }

        _logger.LogInformation(
            "Hybrid wallet+VNPay payment initiated for OrderId={OrderId}, WalletPortion={WalletPortion}, VnPayPortion={VnPayPortion}",
            order.Id.Value, walletPortion, vnpayPortion);

        return new CheckoutOrderResponse(
            TransactionId: urlResult.Value.TransactionId,
            TransactionRef: urlResult.Value.TransactionRef,
            PaymentUrl: urlResult.Value.PaymentUrl);
    }

}
