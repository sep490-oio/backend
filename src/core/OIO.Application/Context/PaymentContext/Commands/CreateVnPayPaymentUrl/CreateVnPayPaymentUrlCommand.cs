using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using Currencies = OIO.Domain.Context.Shared.Enums.Currency;

namespace OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;

/// <summary>
/// Command tạo URL thanh toán VNPay.
/// Frontend gọi endpoint này → nhận paymentUrl → redirect user sang VNPay.
/// </summary>
public sealed record CreateVnPayPaymentUrlCommand(
    decimal Amount,
    string Currency,
    string Purpose,
    IPAddress IpAddress,
    string Description,
    string? BankCode = null,
    Guid? AuctionId = null,
    Guid? OrderId = null,
    Guid? BuyNowReservationId = null,
    Guid? PaymentMethodId = null,
    bool SaveCard = false,
    string? CardType = null,
    string? ClientReturnPath = null) : ICommand<CreateVnPayPaymentUrlResponse>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateVnPayPaymentUrlCommand.Check()
            .WithOwnerName("CreateVnPayPaymentUrl")
            .Field(Amount)
            .NonNegative()
            .Field(Currency)
            .NotWhiteSpace()
            .InSet(Currencies.All.Select(x => x.Id))
            .Field(Description)
            .NotWhiteSpace()
            .Field(Purpose)
            .NotWhiteSpace()
            .InSet(PaymentPurpose.All.Select(x => x.Id));
    }
}

public sealed record CreateVnPayPaymentUrlResponse(
    Guid TransactionId,
    string TransactionRef,
    string PaymentUrl);

internal sealed class CreateVnPayPaymentUrlCommandHandler
    : ICommandHandler<CreateVnPayPaymentUrlCommand, CreateVnPayPaymentUrlResponse>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateVnPayPaymentUrlCommandHandler(
        IPaymentGatewayService paymentGateway,
        ICurrentUser currentUser,
        IClock clock,
        IDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _paymentGateway = paymentGateway;
        _currentUser = currentUser;
        _clock = clock;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateVnPayPaymentUrlResponse, Error>> Handle(
        CreateVnPayPaymentUrlCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var purpose = PaymentPurpose.FromId(request.Purpose).Value;

        if (purpose == PaymentPurpose.AuctionDeposit)
        {
            if (!request.AuctionId.HasValue)
            {
                return Error.Validation(
                    "AuctionId",
                    "AuctionDeposit.AuctionIdRequired",
                    "AuctionId is required when creating an auction deposit payment.");
            }

            var auctionId = AuctionId.From(request.AuctionId.Value);
            var auction = await _dbContext.Set<Auction>()
                .AsNoTracking()
                .Include(a => a.Item)
                .Include(a => a.Deposits)
                .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

            if (auction is null)
                return AuctionErrors.Auction.NotFound(auctionId);

            if (auction.Item.SellerId == _currentUser.UserId)
                return AuctionErrors.Auction.SelfBid;

            // IsPostWinnerTransient — pure C# guard, use helper directly.
            // Completed is also terminal and must be blocked from new deposits.
            if (auction.Status == AuctionStatus.Cancelled ||
                auction.Status == AuctionStatus.Ended ||
                auction.Status.IsPostWinnerTransient ||
                auction.Status == AuctionStatus.Completed ||
                auction.Status == AuctionStatus.Failed)
            {
                return AuctionErrors.Auction.InvalidState(auction.Status.Id, "create auction deposit");
            }

            if (auction.Info is null)
                return AuctionErrors.Auction.TimingRequired;

            if (!auction.Info.HasQualification)
                return AuctionErrors.Auction.QualificationWindowRequired;

            if (auction.Info.IsQualificationClosed(now))
                return AuctionErrors.Participant.JoinWindowClosed;

            if (!auction.Info.IsQualificationOpen(now))
                return AuctionErrors.Participant.JoinWindowNotOpenYet;

            if (auction.Deposits.Any(d => d.BidderId == _currentUser.UserId && d.IsHeld))
            {
                return Error.Conflict(
                    "AuctionDeposit.AlreadyHeld",
                    "An active deposit already exists for this auction.");
            }
        }
        else if (request.Purpose == PaymentPurpose.AuctionBuyNow)
        {
            if (!request.AuctionId.HasValue)
            {
                return Error.Validation(
                    "AuctionId",
                    "AuctionBuyNow.AuctionIdRequired",
                    "AuctionId is required when creating an auction buy-now payment.");
            }

            if (!request.BuyNowReservationId.HasValue)
            {
                return Error.Validation(
                    "BuyNowReservationId",
                    "AuctionBuyNow.ReservationIdRequired",
                    "BuyNowReservationId is required when creating an auction buy-now payment.");
            }
        }

        // 1. Tạo transaction reference duy nhất
        var txnRef = $"{now:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..36];

        // 2. Tạo Money value object
        var (_, isMoneyFailure, money, moneyError) = Money.Create(request.Amount, request.Currency);
        if (isMoneyFailure)
            return moneyError;

        // 3. Tạo TransactionNumber value object
        var (_, isTxnNumFailure, txnNumber, txnNumError) = TransactionNumber.Create(txnRef);
        if (isTxnNumFailure)
            return txnNumError;

        // 4. Map PaymentPurpose → TransactionType
        var transactionType = purpose switch
        {
            _ when purpose == PaymentPurpose.AuctionDeposit => TransactionType.Deposit,
            _ when purpose == PaymentPurpose.OrderPayment => TransactionType.Payment,
            _ when purpose == PaymentPurpose.AuctionBuyNow => TransactionType.Payment,
            _ when purpose == PaymentPurpose.WalletTopUp => TransactionType.Deposit,
            _ => TransactionType.Payment,
        };

        // 5. Idempotency Check: Kiểm tra xem đã có giao dịch Pending nào cho Order/Auction này chưa
        Transaction? transaction = null;

        if (request.Purpose == PaymentPurpose.OrderPayment && request.OrderId.HasValue)
        {
            // Retry policy for order payment: never reuse a stale txnRef on VNPay.
            // Any pre-existing pending transaction for the same (user, order) is
            // cancelled here, and a fresh transaction + new txnRef is created below.
            var orderId = OrderId.From(request.OrderId.Value);
            var stalePending = await _dbContext.Set<Transaction>()
                .Where(t =>
                    t.UserId == _currentUser.UserId &&
                    t.OrderId! == orderId &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Payment)
                .ToListAsync(cancellationToken);

            foreach (var stale in stalePending)
            {
                var cancelResult = stale.CancelPending("retry_payment_replaced", now);
                if (cancelResult.IsFailure)
                    return cancelResult.Error;
            }

            if (stalePending.Count > 0)
                await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Leave `transaction` null so the block below always creates a new one.
        }
        else if (request.Purpose == PaymentPurpose.AuctionDeposit && request.AuctionId.HasValue)
        {
            var pendingAuctionId = AuctionId.From(request.AuctionId.Value);
            var depositDescPrefix = $"[{PaymentPurpose.AuctionDeposit}]";
            transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(t => 
                    t.UserId == _currentUser.UserId &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Deposit &&
                    ((t.AuctionId.HasValue && t.AuctionId.Value == pendingAuctionId) ||
                     (t.Description != null && t.Description.StartsWith(depositDescPrefix) &&
                      t.Description.Contains(request.AuctionId.Value.ToString()))), 
                    cancellationToken);
        }
        else if (request.Purpose == PaymentPurpose.AuctionBuyNow && request.BuyNowReservationId.HasValue)
        {
            var pendingReservationId = AuctionBuyNowReservationId.From(request.BuyNowReservationId.Value);
            var buyNowDescPrefix = $"[{PaymentPurpose.AuctionBuyNow}]";
            transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(t =>
                    t.UserId == _currentUser.UserId &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Payment &&
                    ((t.BuyNowReservationId.HasValue && t.BuyNowReservationId.Value == pendingReservationId) ||
                     (t.Description != null && t.Description.StartsWith(buyNowDescPrefix) &&
                      t.Description.Contains(request.BuyNowReservationId.Value.ToString()))),
                    cancellationToken);
        }

        // Nếu chưa có giao dịch Pending, tạo mới
        if (transaction is null)
        {
            var description = $"[{request.Purpose}] {request.Description}";
            if (request.AuctionId.HasValue)
            {
                description += $" - AuctionId: {request.AuctionId.Value}";
            }

            if (request.BuyNowReservationId.HasValue)
            {
                description += $" - ReservationId: {request.BuyNowReservationId.Value}";
            }

            var (_, isTxnFailure, newTransaction, txnError) = Transaction.Create(
                userId: _currentUser.UserId,
                transactionNumber: txnNumber,
                type: transactionType,
                amount: money,
                currency: request.Currency,
                description: description,
                nowUtc: now,
                orderId: request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null,
                auctionId: request.AuctionId.HasValue ? AuctionId.From(request.AuctionId.Value) : null,
                buyNowReservationId: request.BuyNowReservationId.HasValue
                    ? AuctionBuyNowReservationId.From(request.BuyNowReservationId.Value)
                    : null);

            if (isTxnFailure)
                return txnError;

            transaction = newTransaction;
            _dbContext.Insert(transaction);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            // Tái sử dụng transaction cũ, cập nhật lại txnRef (TransactionNumber mới) để generate URL mới cho chuẩn VNPay
            var (_, updateTxnNumFailure, newTxnNum, updateError) = TransactionNumber.Create(txnRef);
            if (updateTxnNumFailure) return updateError;

            // Transaction domain model should ideally have an UpdateTransactionNumber method for true DDD,
            // but for idempotency on a Pending transaction, regenerating URL with the original TransactionRef is safer.
            txnRef = transaction.TransactionNumber.Value;
        }

        transaction.SetClientReturnPath(request.ClientReturnPath);

        if (request.SaveCard && string.IsNullOrWhiteSpace(request.CardType))
            return Error.Validation("CardType", "Payment.CardTypeRequired",
                "Card type is required when saving card.");

        // 6. Tạo URL thanh toán VNPay — route theo PaymentMethodId / SaveCard
        var amountVnd = (long)request.Amount;
        var ipStr = request.IpAddress.ToString();
        var appUserId = _currentUser.UserId.Value.ToString();

        Result<CreatePaymentUrlResult, Error> urlResult;

        if (request.PaymentMethodId.HasValue)
        {
            // Thanh toán bằng token đã lưu
            var paymentMethod = await _dbContext.Set<PaymentMethod>()
                .FirstOrDefaultAsync(
                    p => p.Id == PaymentMethodId.From(request.PaymentMethodId.Value) &&
                         p.UserId == _currentUser.UserId &&
                         p.IsActive &&
                         p.Type == PaymentMethodType.VnPay,
                    cancellationToken);

            if (paymentMethod is null)
                return Error.NotFound("PaymentMethod.NotFound", "Active VNPay payment method not found.");

            if (string.IsNullOrWhiteSpace(paymentMethod.VnPayToken))
                return Error.Validation("PaymentMethod.NoToken", "PaymentMethod.TokenRequired",
                    "Selected payment method does not have a VNPay token.");

            transaction.AssociatePaymentMethod(paymentMethod.Id);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            urlResult = _paymentGateway.CreateTokenPayUrl(new TokenPaymentUrlRequest
            {
                TransactionRef = txnRef,
                Amount = amountVnd,
                OrderDescription = request.Description,
                AppUserId = appUserId,
                Token = paymentMethod.VnPayToken,
                IpAddress = ipStr,
            });
        }
        else if (request.SaveCard)
        {
            // Thanh toán lần đầu + lưu token. CardType must be spec code (01/02).
            var resolvedCardType = request.CardType?.Trim() is "01" or "02" ? request.CardType!.Trim() : "01";
            urlResult = _paymentGateway.CreatePayAndCreateTokenUrl(new CreateTokenPaymentUrlRequest
            {
                TransactionRef = txnRef,
                Amount = amountVnd,
                OrderDescription = request.Description,
                AppUserId = appUserId,
                IpAddress = ipStr,
                CardType = resolvedCardType,
            });
        }
        else
        {
            // Flow thanh toán thường (không token)
            urlResult = _paymentGateway.CreatePaymentUrl(new CreatePaymentUrlRequest
            {
                TransactionRef = txnRef,
                Amount = amountVnd,
                OrderDescription = request.Description,
                Purpose = purpose,
                IpAddress = ipStr,
                BankCode = request.BankCode,
            });
        }

        if (urlResult.IsFailure)
            return urlResult.Error;

        return new CreateVnPayPaymentUrlResponse(
            TransactionId: transaction.Id.Value,
            TransactionRef: txnRef,
            PaymentUrl: urlResult.Value.PaymentUrl);
    }
}
