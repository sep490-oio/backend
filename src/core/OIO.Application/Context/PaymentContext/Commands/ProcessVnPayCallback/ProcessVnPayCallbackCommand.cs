using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.ProcessVnPayCallback;

/// <summary>
/// Command xử lý VNPay IPN/Return callback.
/// Validate signature → parse kết quả → cập nhật Transaction + thực hiện action tương ứng.
/// </summary>
public sealed record ProcessVnPayCallbackCommand(
    IDictionary<string, string> QueryParams) : ICommand<ProcessVnPayCallbackResponse>;

public sealed record ProcessVnPayCallbackResponse(
    string TransactionRef,
    bool IsSuccess,
    string ResponseCode,
    string Message);

internal sealed class ProcessVnPayCallbackCommandHandler
    : ICommandHandler<ProcessVnPayCallbackCommand, ProcessVnPayCallbackResponse>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ProcessVnPayCallbackCommandHandler> _logger;

    public ProcessVnPayCallbackCommandHandler(
        IPaymentGatewayService paymentGateway,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ProcessVnPayCallbackCommandHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<ProcessVnPayCallbackResponse, Error>> Handle(
        ProcessVnPayCallbackCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Parse & validate callback signature
        var callbackResult = _paymentGateway.ProcessCallback(request.QueryParams);
        if (callbackResult.IsFailure)
            return callbackResult.Error;

        var callback = callbackResult.Value;
        var now = _clock.UtcNow;

        _logger.LogInformation(
            "VNPay callback: TxnRef={TxnRef}, Success={IsSuccess}, ResponseCode={ResponseCode}",
            callback.TransactionRef, callback.IsSuccess, callback.ResponseCode);

        // 2. Tìm Transaction trong DB theo TransactionRef (TransactionNumber)
        var transaction = await _dbContext.Set<Transaction>()
            .FirstOrDefaultAsync(
                t => t.TransactionNumber.Value == callback.TransactionRef,
                cancellationToken);

        if (transaction is null)
        {
            _logger.LogWarning("Transaction not found for TxnRef={TxnRef}", callback.TransactionRef);
            return Error.NotFound("Transaction.NotFound",
                $"Transaction with ref '{callback.TransactionRef}' not found.");
        }

        // 3. Kiểm tra idempotent — nếu đã xử lý rồi thì bỏ qua
        if (transaction.Status == TransactionStatus.Completed ||
            transaction.Status == TransactionStatus.Failed)
        {
            _logger.LogInformation(
                "Transaction {TxnRef} already processed with status {Status}",
                callback.TransactionRef, transaction.Status);

            return new ProcessVnPayCallbackResponse(
                TransactionRef: callback.TransactionRef,
                IsSuccess: transaction.Status == TransactionStatus.Completed,
                ResponseCode: callback.ResponseCode,
                Message: "Transaction already processed.");
        }

        // 4. Tạo GatewayInfo từ callback
        var gatewayInfo = GatewayInfo.Create(
            provider: "vnpay",
            transactionId: callback.VnPayTransactionNo,
            response: callback.RawResponseJson);

        if (callback.IsSuccess)
        {
            // 5a. Thanh toán thành công
            var markResult = transaction.MarkAsCompleted(gatewayInfo, now);
            if (markResult.IsFailure)
                return markResult.Error;

            // 6. Xử lý theo PaymentPurpose (lấy từ Description prefix)
            var description = transaction.Description ?? "";

            if (description.StartsWith("[AuctionDeposit]"))
            {
                await HandleAuctionDepositAsync(transaction, now, cancellationToken);
            }
            else if (description.StartsWith("[AuctionBuyNow]"))
            {
                var buyNowResult = await HandleAuctionBuyNowAsync(transaction, now, cancellationToken);
                if (buyNowResult.IsFailure)
                    return buyNowResult.Error;
            }
            else if (description.StartsWith("[OrderPayment]"))
            {
                await HandleOrderPaymentAsync(transaction, now, cancellationToken);
            }
            else if (description.StartsWith("[WalletTopUp]"))
            {
                await HandleWalletTopUpAsync(transaction, now, cancellationToken);
            }
            else
            {
                // Default: treat as wallet top-up
                await HandleWalletTopUpAsync(transaction, now, cancellationToken);
            }

            _logger.LogInformation("Payment completed for TxnRef={TxnRef}", callback.TransactionRef);
        }
        else
        {
            // 5b. Thanh toán thất bại
            var markResult = transaction.MarkAsFailed(gatewayInfo, now);
            if (markResult.IsFailure)
                return markResult.Error;

            var description = transaction.Description ?? string.Empty;
            if (description.StartsWith("[AuctionBuyNow]"))
            {
                var failReservationResult = await HandleAuctionBuyNowFailedAsync(transaction, now, cancellationToken);
                if (failReservationResult.IsFailure)
                    return failReservationResult.Error;
            }

            _logger.LogWarning(
                "Payment failed for TxnRef={TxnRef}, ResponseCode={ResponseCode}",
                callback.TransactionRef, callback.ResponseCode);
        }

        // 7. Persist tất cả changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProcessVnPayCallbackResponse(
            TransactionRef: callback.TransactionRef,
            IsSuccess: callback.IsSuccess,
            ResponseCode: callback.ResponseCode,
            Message: callback.IsSuccess
                ? "Thanh toán thành công"
                : $"Thanh toán thất bại (mã: {callback.ResponseCode})");
    }

    /// <summary>
    /// Luồng Đặt cọc đấu giá: VNPay → Wallet.Credit → AuctionDeposit.Create
    /// </summary>
    private async Task HandleAuctionDepositAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        // Tìm wallet của user
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet is null)
        {
            _logger.LogError("Wallet not found for UserId={UserId}", transaction.UserId);
            return;
        }

        // Nạp tiền vào wallet
        var creditResult = wallet.Credit(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"VNPay deposit for auction - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (creditResult.IsFailure)
        {
            _logger.LogError("Failed to credit wallet: {Error}", creditResult.Error.Message);
            return;
        }

        // Hold auction deposit inside wallet pending balance
        var debitResult = wallet.Hold(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"Auction deposit hold - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (debitResult.IsFailure)
        {
            _logger.LogError("Failed to hold wallet funds for deposit: {Error}", debitResult.Error.Message);
            return;
        }

        // Lấy AuctionId từ Description
        // Format: "[AuctionDeposit] {desc} - AuctionId: {guid}"
        Guid? parsedAuctionId = null;
        if (transaction.Description != null)
        {
            var parts = transaction.Description.Split("AuctionId: ");
            if (parts.Length > 1 && Guid.TryParse(parts[1].Trim(), out var guid))
            {
                parsedAuctionId = guid;
            }
        }

        if (parsedAuctionId.HasValue)
        {
            var auctionId = AuctionId.From(parsedAuctionId.Value);
            var auction = await _dbContext.Set<Auction>()
                .Include(a => a.Item)
                .Include(a => a.Participants)
                .FirstOrDefaultAsync(a => a.Id == auctionId, ct);

            if (auction is null)
            {
                var notFoundError = AuctionErrors.Auction.NotFound(auctionId);
                _logger.LogError(
                    "Failed to register deposit participation for AuctionId={AuctionId}, TxnRef={TxnRef}: {Error}",
                    parsedAuctionId.Value,
                    transaction.TransactionNumber.Value,
                    notFoundError.Message);
                return;
            }

            var depositResult = AuctionDeposit.Create(
                auctionId,
                transaction.UserId,
                transaction.Amount,
                transaction.Id,
                now);

            if (depositResult.IsFailure)
            {
                _logger.LogError("Failed to create AuctionDeposit: {Error}", depositResult.Error.Message);
                return;
            }

            _dbContext.Insert(depositResult.Value);

            var participantResult = auction.RegisterParticipantFromDeposit(transaction.UserId, now);
            if (participantResult.IsFailure)
            {
                _logger.LogError(
                    "AuctionDeposit created but participant registration failed for AuctionId={AuctionId}, UserId={UserId}, TxnRef={TxnRef}: {Error}",
                    parsedAuctionId.Value,
                    transaction.UserId.Value,
                    transaction.TransactionNumber.Value,
                    participantResult.Error.Message);
                return;
            }

            _logger.LogInformation(
                "AuctionDeposit created and participant registered for AuctionId={AuctionId}, UserId={UserId}, TxnRef={TxnRef}",
                parsedAuctionId.Value,
                transaction.UserId.Value,
                transaction.TransactionNumber.Value);
        }
        else
        {
            _logger.LogWarning("Could not parse AuctionId from Transaction Description: {Desc}", transaction.Description);
        }
    }

    /// <summary>
    /// Luồng thanh toán đơn hàng: VNPay → Transaction.Completed → Order.MarkAsPaid & Escrow.Create
    /// </summary>
    private async Task HandleOrderPaymentAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        if (transaction.OrderId is null)
        {
            _logger.LogWarning("OrderId is null for OrderPayment transaction {TxnRef}", transaction.TransactionNumber.Value);
            return;
        }

        // 1. Create Escrow to hold the funds
        var escrowResult = Escrow.Create(
            transaction.OrderId.Value,
            transaction.Id,
            transaction.Amount,
            transaction.Currency,
            now);

        if (escrowResult.IsSuccess)
        {
            _dbContext.Insert(escrowResult.Value);
            _logger.LogInformation("Escrow holding created for OrderId={OrderId}, TxnRef={TxnRef}", transaction.OrderId.Value, transaction.TransactionNumber.Value);
        }
        else
        {
            _logger.LogError("Failed to create Escrow: {Error}", escrowResult.Error.Message);
        }

        // 2. Load Order and Mark As Paid
        var order = await _dbContext.Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
            .FirstOrDefaultAsync(o => o.Id == transaction.OrderId.Value, ct);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found while processing payment via Transaction {TxnRef}", transaction.OrderId.Value, transaction.TransactionNumber.Value);
            return;
        }

        var markPaidResult = order.MarkAsPaid(now);

        if (markPaidResult.IsFailure)
        {
             _logger.LogError("Failed to mark Order {OrderId} as paid: {Error}", order.Id, markPaidResult.Error.Message);
             return;
        }

        _logger.LogInformation("Order {OrderId} marked as Paid successfully", order.Id);
    }

    /// <summary>
    /// Luồng nạp tiền ví: VNPay → Wallet.Credit
    /// </summary>
    private async Task<UnitResult<Error>> HandleAuctionBuyNowAsync(
        Transaction transaction,
        DateTime now,
        CancellationToken ct)
    {
        var reservation = await _dbContext.Set<AuctionBuyNowReservation>()
            .Include(x => x.Auction)
                .ThenInclude(x => x.Item)
            .Include(x => x.Auction.Bids)
            .Include(x => x.Auction.AutoBids)
            .Include(x => x.Auction.Deposits)
            .Include(x => x.Auction.Participants)
            .Include(x => x.Auction.BuyNowReservations)
            .FirstOrDefaultAsync(x => x.PaymentTransactionId == transaction.Id, ct);

        if (reservation is null)
        {
            _logger.LogWarning(
                "Buy-now reservation not found for payment transaction {TxnRef}",
                transaction.TransactionNumber.Value);
            return Error.NotFound("AuctionBuyNowReservation.NotFound", "Buy-now reservation was not found for the callback transaction.");
        }

        var auction = reservation.Auction;
        var buyer = await _dbContext.Set<User>()
            .Include(x => x.Profile)
            .Include(x => x.Addresses)
            .FirstOrDefaultAsync(x => x.Id == reservation.BuyerId, ct);

        if (buyer is null)
        {
            _logger.LogError(
                "Buy-now buyer {BuyerId} not found for reservation {ReservationId}",
                reservation.BuyerId.Value,
                reservation.Id.Value);
            return Error.NotFound("User.NotFound", "Buyer was not found for the buy-now reservation.");
        }

        if (!reservation.IsActive(now))
        {
            var creditLateResult = await CreditLateBuyNowPaymentToWalletAsync(transaction, now, ct);
            if (creditLateResult.IsFailure)
                return creditLateResult.Error;

            var lateFailResult = auction.FailBuyNowReservation(
                reservation.Id,
                "late_payment_success",
                now);

            if (lateFailResult.IsFailure)
                return lateFailResult.Error;

            return UnitResult.Success<Error>();
        }

        var order = CreateBuyNowOrder(auction, buyer, reservation, now);
        if (order.IsFailure)
        {
            _logger.LogError(
                "Failed to create order for buy-now reservation {ReservationId}: {Error}",
                reservation.Id.Value,
                order.Error.Message);

            var creditLateResult = await CreditLateBuyNowPaymentToWalletAsync(transaction, now, ct);
            if (creditLateResult.IsFailure)
                return creditLateResult.Error;

            var failOrderResult = auction.FailBuyNowReservation(
                reservation.Id,
                "order_creation_failed_after_payment",
                now);

            if (failOrderResult.IsFailure)
                return failOrderResult.Error;

            return UnitResult.Success<Error>();
        }

        var finalizeResult = auction.FinalizeBuyNowReservation(
            reservation.Id,
            now);

        if (finalizeResult.IsFailure)
        {
            _logger.LogWarning(
                "Could not finalize buy-now reservation {ReservationId}: {Error}",
                reservation.Id.Value,
                finalizeResult.Error.Message);

            var creditLateResult = await CreditLateBuyNowPaymentToWalletAsync(transaction, now, ct);
            if (creditLateResult.IsFailure)
                return creditLateResult.Error;

            var failFinalizeResult = auction.FailBuyNowReservation(
                reservation.Id,
                "buy_now_finalize_failed_after_payment",
                now);

            if (failFinalizeResult.IsFailure)
                return failFinalizeResult.Error;

            return UnitResult.Success<Error>();
        }

        _dbContext.Insert(order.Value);

        var linkResult = auction.LinkBuyNowReservationOrder(reservation.Id, order.Value.Id, now);
        if (linkResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to link buy-now reservation {ReservationId} to order {OrderId}: {Error}",
                reservation.Id.Value,
                order.Value.Id.Value,
                linkResult.Error.Message);
        }

        if (transaction.Amount.Amount > 0)
        {
            var gatewayEscrowResult = Escrow.Create(
                order.Value.Id,
                transaction.Id,
                transaction.Amount,
                transaction.Currency,
                now);

            if (gatewayEscrowResult.IsSuccess)
            {
                _dbContext.Insert(gatewayEscrowResult.Value);
            }
            else
            {
                _logger.LogError(
                    "Failed to create gateway escrow for buy-now order {OrderId}: {Error}",
                    order.Value.Id.Value,
                    gatewayEscrowResult.Error.Message);
                return gatewayEscrowResult.Error;
            }
        }

        if (reservation.DepositAppliedAmount.Amount > 0)
        {
            var depositFundingResult = await ApplyBuyNowDepositFundingAsync(
                auction,
                order.Value,
                reservation,
                now,
                ct);

            if (depositFundingResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to apply buy-now deposit funding for reservation {ReservationId}: {Error}",
                    reservation.Id.Value,
                    depositFundingResult.Error.Message);
                return depositFundingResult.Error;
            }
        }

        var markPaidResult = order.Value.MarkAsPaid(now);
        if (markPaidResult.IsFailure)
        {
            _logger.LogError(
                "Failed to mark buy-now order {OrderId} as paid: {Error}",
                order.Value.Id.Value,
                markPaidResult.Error.Message);
            return markPaidResult.Error;
        }

        return UnitResult.Success<Error>();
    }

    private async Task<UnitResult<Error>> HandleAuctionBuyNowFailedAsync(
        Transaction transaction,
        DateTime now,
        CancellationToken ct)
    {
        var reservation = await _dbContext.Set<AuctionBuyNowReservation>()
            .Include(x => x.Auction)
                .ThenInclude(x => x.BuyNowReservations)
            .FirstOrDefaultAsync(x => x.PaymentTransactionId == transaction.Id, ct);

        if (reservation is null)
            return UnitResult.Success<Error>();

        var failResult = reservation.Auction.FailBuyNowReservation(
            reservation.Id,
            "payment_failed",
            now);

        if (failResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to mark buy-now reservation {ReservationId} as payment failed: {Error}",
                reservation.Id.Value,
                failResult.Error.Message);
            return failResult.Error;
        }

        return UnitResult.Success<Error>();
    }

    private Result<Order, Error> CreateBuyNowOrder(
        Auction auction,
        User buyer,
        AuctionBuyNowReservation reservation,
        DateTime nowUtc)
    {
        var shippingAddress = buyer.Addresses.FirstOrDefault(x => x.IsDefault)
                              ?? buyer.Addresses.FirstOrDefault();

        var shipping = shippingAddress is null
            ? ShippingSnapshot.Create(
                recipientName: ResolveUserDisplayName(buyer),
                phone: null,
                address: "Address pending update",
                ward: null,
                district: null,
                city: null)
            : ShippingSnapshot.Create(
                recipientName: shippingAddress.Recipient.RecipientName,
                phone: shippingAddress.Recipient.Phone.Value,
                address: shippingAddress.Address.Street,
                ward: shippingAddress.Address.Ward,
                district: shippingAddress.Address.District,
                city: shippingAddress.Address.City);

        var pricing = OrderPricing.Create(
            itemPrice: reservation.BuyNowPrice,
            shippingFee: 0m,
            platformFee: 0m,
            taxAmount: 0m,
            totalAmount: reservation.BuyNowPrice);

        return Order.Create(
            auctionId: auction.Id,
            buyerId: buyer.Id,
            sellerId: auction.Item.SellerId,
            shipping: shipping,
            shippingAddressId: shippingAddress?.Id,
            billingAddressId: shippingAddress?.Id,
            pricing: pricing,
            currency: reservation.BuyNowPrice.Currency.Id,
            paymentDueAt: nowUtc,
            nowUtc: nowUtc,
            notes: "Created from buy-now payment callback.");
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
            description: $"[AuctionBuyNowDepositApplied] AuctionId: {auction.Id.Value} - ReservationId: {reservation.Id.Value} - OrderId: {order.Id.Value}",
            nowUtc: now,
            orderId: order.Id);

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
            $"Auction buy-now deposit applied for reservation {reservation.Id.Value}",
            now);

        if (debitPendingResult.IsFailure)
        {
            var debitResult = wallet.Debit(
                reservation.DepositAppliedAmount.Amount,
                fundingTx.Value.Id,
                $"Auction buy-now deposit applied for reservation {reservation.Id.Value}",
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

    private async Task<UnitResult<Error>> CreditLateBuyNowPaymentToWalletAsync(
        Transaction transaction,
        DateTime now,
        CancellationToken ct)
    {
        if (transaction.Amount.Amount <= 0)
            return UnitResult.Success<Error>();

        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.UserId == transaction.UserId, ct);

        if (wallet is null)
        {
            _logger.LogWarning(
                "Wallet not found for late buy-now payment credit. UserId={UserId}",
                transaction.UserId.Value);
            return Error.NotFound("Wallet.NotFound", "Wallet not found for late buy-now payment credit.");
        }

        var creditResult = wallet.Credit(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"Late buy-now payment credited to wallet - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (creditResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to credit wallet for late buy-now payment {TxnRef}: {Error}",
                transaction.TransactionNumber.Value,
                creditResult.Error.Message);
            return creditResult.Error;
        }

        return UnitResult.Success<Error>();
    }

    private static string ResolveUserDisplayName(User user)
    {
        var displayName = user.Profile?.Name?.DisplayName?.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        var fullName = user.Profile?.Name?.FullName?.Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return user.UserName.Value;
    }

    private async Task HandleWalletTopUpAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet is null)
        {
            _logger.LogError("Wallet not found for UserId={UserId}", transaction.UserId);
            return;
        }

        var creditResult = wallet.Credit(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"VNPay wallet top-up - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (creditResult.IsFailure)
        {
            _logger.LogError("Failed to credit wallet: {Error}", creditResult.Error.Message);
            return;
        }

        _logger.LogInformation(
            "Wallet top-up completed for UserId={UserId}, Amount={Amount}",
            transaction.UserId, transaction.Amount.Amount);
    }
}
