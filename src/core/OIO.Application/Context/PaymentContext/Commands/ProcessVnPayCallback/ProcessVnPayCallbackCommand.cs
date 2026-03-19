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
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.ProcessVnPayCallback;

/// <summary>
/// Command xá»­ lÃ½ VNPay IPN/Return callback.
/// Validate signature â†’ parse káº¿t quáº£ â†’ cáº­p nháº­t Transaction + thá»±c hiá»‡n action tÆ°Æ¡ng á»©ng.
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

        // 2. TÃ¬m Transaction trong DB theo TransactionRef (TransactionNumber)
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

        // 3. Kiá»ƒm tra idempotent â€” náº¿u Ä‘Ã£ xá»­ lÃ½ rá»“i thÃ¬ bá» qua
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

        // 4. Táº¡o GatewayInfo tá»« callback
        var gatewayInfo = GatewayInfo.Create(
            provider: "vnpay",
            transactionId: callback.VnPayTransactionNo,
            response: callback.RawResponseJson);
        var purpose = ResolvePaymentPurpose(transaction);

        return callback.IsSuccess
            ? await HandleSuccessfulCallbackAsync(
                callback,
                transaction,
                gatewayInfo,
                purpose,
                now,
                cancellationToken)
            : await HandleFailedCallbackAsync(
                callback,
                transaction,
                gatewayInfo,
                purpose,
                now,
                cancellationToken);

    }

    private async Task<Result<ProcessVnPayCallbackResponse, Error>> HandleSuccessfulCallbackAsync(
        PaymentCallbackResult callback,
        Transaction transaction,
        GatewayInfo gatewayInfo,
        PaymentPurpose purpose,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var successResult = purpose switch
        {
            _ when purpose == PaymentPurpose.AuctionDeposit => await HandleAuctionDepositAsync(transaction, now, cancellationToken),
            _ when purpose == PaymentPurpose.AuctionBuyNow => await HandleAuctionBuyNowAsync(transaction, now, cancellationToken),
            _ when purpose == PaymentPurpose.OrderPayment => await HandleOrderPaymentAsync(transaction, now, cancellationToken),
            _ => await HandleWalletTopUpAsync(transaction, now, cancellationToken),
        };

        if (successResult.IsFailure)
            return successResult.Error;

        var markResult = transaction.MarkAsCompleted(gatewayInfo, now);
        if (markResult.IsFailure)
            return markResult.Error;

        // Auto-create hoặc link PaymentMethod từ VNPay token (pay_and_create / token_pay)
        await TryLinkOrCreatePaymentMethodFromTokenAsync(callback, transaction, now, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Payment completed for TxnRef={TxnRef}", callback.TransactionRef);

        return new ProcessVnPayCallbackResponse(
            TransactionRef: callback.TransactionRef,
            IsSuccess: true,
            ResponseCode: callback.ResponseCode,
            Message: "Thanh toan thanh cong");
    }

    private async Task<Result<ProcessVnPayCallbackResponse, Error>> HandleFailedCallbackAsync(
        PaymentCallbackResult callback,
        Transaction transaction,
        GatewayInfo gatewayInfo,
        PaymentPurpose purpose,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var markResult = transaction.MarkAsFailed(gatewayInfo, now);
        if (markResult.IsFailure)
            return markResult.Error;

        if (purpose == PaymentPurpose.AuctionBuyNow)
        {
            var failReservationResult = await HandleAuctionBuyNowFailedAsync(transaction, now, cancellationToken);
            if (failReservationResult.IsFailure)
                return failReservationResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Payment failed for TxnRef={TxnRef}, ResponseCode={ResponseCode}",
            callback.TransactionRef,
            callback.ResponseCode);

        return new ProcessVnPayCallbackResponse(
            TransactionRef: callback.TransactionRef,
            IsSuccess: false,
            ResponseCode: callback.ResponseCode,
            Message: $"Thanh toan that bai (ma: {callback.ResponseCode})");
    }

    private static PaymentPurpose ResolvePaymentPurpose(Transaction transaction)
    {
        if (transaction.BuyNowReservationId.HasValue)
            return PaymentPurpose.AuctionBuyNow;

        if (transaction.AuctionId.HasValue && transaction.Type == TransactionType.Deposit)
            return PaymentPurpose.AuctionDeposit;

        if (transaction.OrderId.HasValue)
            return PaymentPurpose.OrderPayment;

        var description = transaction.Description ?? string.Empty;
        if (description.StartsWith("[AuctionDeposit]", StringComparison.OrdinalIgnoreCase))
            return PaymentPurpose.AuctionDeposit;

        if (description.StartsWith("[AuctionBuyNow]", StringComparison.OrdinalIgnoreCase))
            return PaymentPurpose.AuctionBuyNow;

        if (description.StartsWith("[OrderPayment]", StringComparison.OrdinalIgnoreCase))
            return PaymentPurpose.OrderPayment;

        return PaymentPurpose.WalletTopUp;
    }

    private async Task<UnitResult<Error>> HandleAuctionDepositAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        if (!transaction.AuctionId.HasValue)
        {
            return Error.Validation(
                "AuctionId",
                "Transaction.AuctionIdRequired",
                "AuctionId is required for an auction deposit callback.");
        }

        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
            .Include(a => a.Deposits)
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == transaction.AuctionId.Value, ct);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(transaction.AuctionId.Value);

        if (auction.Item.SellerId == transaction.UserId)
            return AuctionErrors.Auction.SelfBid;

        if (auction.Status == Domain.Context.AuctionContext.Enums.AuctionStatus.Cancelled ||
            auction.Status == Domain.Context.AuctionContext.Enums.AuctionStatus.Ended ||
            auction.Status == Domain.Context.AuctionContext.Enums.AuctionStatus.Sold ||
            auction.Status == Domain.Context.AuctionContext.Enums.AuctionStatus.Failed ||
            auction.Status == Domain.Context.AuctionContext.Enums.AuctionStatus.Terminated)
        {
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "process auction deposit callback");
        }

        if (auction.Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!auction.Info.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;

        if (!auction.Info.IsQualificationOpen(now))
            return AuctionErrors.Participant.JoinWindowClosed;

        if (auction.Deposits.Any(d => d.BidderId == transaction.UserId && d.IsHeld))
        {
            return Error.Conflict(
                "AuctionDeposit.AlreadyHeld",
                "An active deposit already exists for this auction.");
        }
        // TÃ¬m wallet cá»§a user
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet.NotFound", "Wallet not found for the transaction user.");
        }

        // Náº¡p tiá»n vÃ o wallet
        var creditResult = wallet.Credit(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"VNPay deposit for auction - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (creditResult.IsFailure)
        {
            return creditResult.Error;
        }

        // Hold auction deposit inside wallet pending balance
        var debitResult = wallet.Hold(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"Auction deposit hold - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (debitResult.IsFailure)
            return debitResult.Error;

        // Láº¥y AuctionId tá»« Description
        // Format: "[AuctionDeposit] {desc} - AuctionId: {guid}"
        var depositResult = AuctionDeposit.Create(
            transaction.AuctionId.Value,
            transaction.UserId,
            transaction.Amount,
            transaction.Id,
            now);

        if (depositResult.IsFailure)
            return depositResult.Error;

        var participantResult = auction.RegisterParticipantFromDeposit(transaction.UserId, now);
        if (participantResult.IsFailure)
            return participantResult.Error;

        _dbContext.Insert(depositResult.Value);

        _logger.LogInformation(
            "AuctionDeposit created and participant registered for AuctionId={AuctionId}, UserId={UserId}, TxnRef={TxnRef}",
            transaction.AuctionId.Value.Value,
            transaction.UserId.Value,
            transaction.TransactionNumber.Value);

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Luá»“ng thanh toÃ¡n Ä‘Æ¡n hÃ ng: VNPay â†’ Transaction.Completed â†’ Order.MarkAsPaid & Escrow.Create
    /// </summary>
    private async Task<UnitResult<Error>> HandleOrderPaymentAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        if (transaction.OrderId is null)
        {
            return Error.Validation(
                "OrderId",
                "Transaction.OrderIdRequired",
                "OrderId is required for an order payment callback.");
        }

        // 1. Load Order
        var order = await _dbContext.Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
            .FirstOrDefaultAsync(o => o.Id == transaction.OrderId.Value, ct);

        if (order is null)
        {
            return Error.NotFound("Order.NotFound", "Order was not found for the payment callback.");
        }

        // 2. Check for hybrid wallet hold — look for a pending hold on the buyer's wallet
        var wallet = await _dbContext.Set<Wallet>()
            .Include(w => w.WalletTransactions)
            .FirstOrDefaultAsync(w => w.UserId == order.BuyerId, ct);

        decimal walletHoldAmount = 0m;
        if (wallet is not null)
        {
            // Detect hybrid hold by checking wallet transactions for a hold with HybridHold marker for this order
            var hybridHoldTx = wallet.WalletTransactions
                .Where(wt => wt.Description != null &&
                             wt.Description.Contains("[HybridHold]") &&
                             wt.Description.Contains(order.Id.Value.ToString()))
                .OrderByDescending(wt => wt.CreatedAt)
                .FirstOrDefault();

            if (hybridHoldTx is not null)
            {
                walletHoldAmount = hybridHoldTx.Amount;
            }
        }

        // 3. Create Escrow for the full order amount (VNPay portion + wallet portion)
        var escrowAmount = transaction.Amount;
        if (walletHoldAmount > 0)
        {
            var fullAmountResult = Money.Create(
                transaction.Amount.Amount + walletHoldAmount,
                transaction.Currency);

            if (fullAmountResult.IsSuccess)
                escrowAmount = fullAmountResult.Value;
        }

        var escrowResult = Escrow.Create(
            transaction.OrderId.Value,
            transaction.Id,
            escrowAmount,
            transaction.Currency,
            now);

        if (escrowResult.IsSuccess)
        {
            _dbContext.Insert(escrowResult.Value);
            _logger.LogInformation("Escrow holding created for OrderId={OrderId}, TxnRef={TxnRef}", transaction.OrderId.Value, transaction.TransactionNumber.Value);
        }
        else
        {
            return escrowResult.Error;
        }

        // 4. Commit hybrid wallet hold if present
        if (walletHoldAmount > 0 && wallet is not null)
        {
            var debitPendingResult = wallet.DebitPending(
                walletHoldAmount,
                transaction.Id,
                $"[HybridHold] Wallet portion committed for order {order.Id.Value}",
                now);

            if (debitPendingResult.IsFailure)
                return debitPendingResult.Error;

            _logger.LogInformation(
                "Hybrid wallet hold committed for OrderId={OrderId}, WalletPortion={WalletPortion}",
                order.Id.Value, walletHoldAmount);
        }

        // 5. Handle winner deposit conversion
        var winnerDeposit = await _dbContext.Set<AuctionDeposit>()
            .FirstOrDefaultAsync(
                d => d.AuctionId == order.AuctionId &&
                     d.BidderId == order.BuyerId &&
                     d.IsHeld,
                ct);

        if (winnerDeposit is not null)
        {
            if (wallet is null)
                return Error.NotFound("Wallet.NotFound", "Winner wallet not found for deposit conversion.");

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

        // 6. Mark order as paid
        var markPaidResult = order.MarkAsPaid(now);

        if (markPaidResult.IsFailure)
        {
             return markPaidResult.Error;
        }

        _logger.LogInformation("Order {OrderId} marked as Paid successfully", order.Id);
        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Luá»“ng náº¡p tiá»n vÃ­: VNPay â†’ Wallet.Credit
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
            .FirstOrDefaultAsync(
                x => (transaction.BuyNowReservationId.HasValue && x.Id == transaction.BuyNowReservationId.Value) ||
                     x.PaymentTransactionId == transaction.Id,
                ct);

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
            .FirstOrDefaultAsync(
                x => (transaction.BuyNowReservationId.HasValue && x.Id == transaction.BuyNowReservationId.Value) ||
                     x.PaymentTransactionId == transaction.Id,
                ct);

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

    /// <summary>
    /// Nếu callback chứa vnp_token (pay_and_create / token_pay), auto-create hoặc update PaymentMethod
    /// và link vào Transaction.
    /// </summary>
    private async Task TryLinkOrCreatePaymentMethodFromTokenAsync(
        PaymentCallbackResult callback,
        Transaction transaction,
        DateTime now,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(callback.VnPayToken))
            return;

        try
        {
            // Tìm PaymentMethod đã tồn tại với cùng token
            var existing = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    p => p.UserId == transaction.UserId &&
                         p.Type == PaymentMethodType.VnPay &&
                         p.VnPayToken == callback.VnPayToken &&
                         p.IsActive,
                    ct);

            if (existing is not null)
            {
                // Update nếu card info thay đổi
                existing.UpdateVnPayToken(
                    callback.VnPayToken,
                    callback.MaskedCardNumber,
                    callback.CardType,
                    callback.BankCode);

                transaction.AssociatePaymentMethod(existing.Id);
            }
            else
            {
                // Auto-create PaymentMethod mới từ token
                var paymentMethod = PaymentMethod.CreateFromVnPayToken(
                    userId: transaction.UserId,
                    vnPayToken: callback.VnPayToken,
                    maskedCardNumber: callback.MaskedCardNumber,
                    vnPayCardType: callback.CardType,
                    bankCode: callback.BankCode,
                    isDefault: false,
                    nowUtc: now);

                _dbContext.Insert(paymentMethod);
                transaction.AssociatePaymentMethod(paymentMethod.Id);

                _logger.LogInformation(
                    "Auto-created VNPay PaymentMethod for UserId={UserId}, BankCode={BankCode}, MaskedCard={MaskedCard}",
                    transaction.UserId.Value,
                    callback.BankCode,
                    callback.MaskedCardNumber);
            }
        }
        catch (Exception ex)
        {
            // Token link là best-effort — không nên fail toàn bộ payment
            _logger.LogWarning(ex,
                "Failed to auto-create/link PaymentMethod from VNPay token for TxnRef={TxnRef}",
                callback.TransactionRef);
        }
    }

    private async Task<UnitResult<Error>> HandleWalletTopUpAsync(
        Transaction transaction, DateTime now, CancellationToken ct)
    {
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == transaction.UserId, ct);

        if (wallet is null)
        {
            return Error.NotFound("Wallet.NotFound", "Wallet not found for the transaction user.");
        }

        var creditResult = wallet.Credit(
            amount: transaction.Amount.Amount,
            transactionId: transaction.Id,
            description: $"VNPay wallet top-up - TxnRef: {transaction.TransactionNumber.Value}",
            nowUtc: now);

        if (creditResult.IsFailure)
        {
            return creditResult.Error;
        }

        _logger.LogInformation(
            "Wallet top-up completed for UserId={UserId}, Amount={Amount}",
            transaction.UserId, transaction.Amount.Amount);

        return UnitResult.Success<Error>();
    }
}
