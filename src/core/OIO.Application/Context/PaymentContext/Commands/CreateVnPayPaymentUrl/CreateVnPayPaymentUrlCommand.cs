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
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.CreateVnPayPaymentUrl;

/// <summary>
/// Command tạo URL thanh toán VNPay.
/// Frontend gọi endpoint này → nhận paymentUrl → redirect user sang VNPay.
/// </summary>
public sealed record CreateVnPayPaymentUrlCommand(
    decimal Amount,
    string Currency,
    PaymentPurpose Purpose,
    IPAddress IpAddress,
    string Description,
    string? BankCode = null,
    Guid? AuctionId = null,
    Guid? OrderId = null,
    Guid? BuyNowReservationId = null) : ICommand<CreateVnPayPaymentUrlResponse>;

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

        if (request.Purpose == PaymentPurpose.AuctionDeposit)
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

            if (auction.Status == AuctionStatus.Cancelled ||
                auction.Status == AuctionStatus.Ended ||
                auction.Status == AuctionStatus.Sold ||
                auction.Status == AuctionStatus.Failed)
            {
                return AuctionErrors.Auction.InvalidState(auction.Status.Id, "create auction deposit");
            }

            if (auction.Info is null)
                return AuctionErrors.Auction.TimingRequired;

            if (!auction.Info.HasQualification)
                return AuctionErrors.Auction.QualificationWindowRequired;

            if (!auction.Info.IsQualificationOpen(now))
                return AuctionErrors.Participant.JoinWindowClosed;

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
        var transactionType = request.Purpose switch
        {
            PaymentPurpose.AuctionDeposit => TransactionType.Deposit,
            PaymentPurpose.OrderPayment => TransactionType.Payment,
            PaymentPurpose.AuctionBuyNow => TransactionType.Payment,
            PaymentPurpose.WalletTopUp => TransactionType.Deposit,
            _ => TransactionType.Payment,
        };

        // 5. Idempotency Check: Kiểm tra xem đã có giao dịch Pending nào cho Order/Auction này chưa
        Transaction? transaction = null;

        if (request.Purpose == PaymentPurpose.OrderPayment && request.OrderId.HasValue)
        {
            transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(t => 
                    t.UserId == _currentUser.UserId &&
                    t.OrderId!.Value == request.OrderId.Value &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Payment, 
                    cancellationToken);
        }
        else if (request.Purpose == PaymentPurpose.AuctionDeposit && request.AuctionId.HasValue)
        {
            // Fallback checking by Description since AuctionId isn't a direct column on Transaction yet.
            // In a real production mapping, an AuctionId column might be added to Transaction.
            var depositDescPrefix = $"[{PaymentPurpose.AuctionDeposit}]";
            transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(t => 
                    t.UserId == _currentUser.UserId &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Deposit &&
                    t.Description != null && t.Description.StartsWith(depositDescPrefix) &&
                    t.Description.Contains(request.AuctionId.Value.ToString()), 
                    cancellationToken);
        }
        else if (request.Purpose == PaymentPurpose.AuctionBuyNow && request.BuyNowReservationId.HasValue)
        {
            var buyNowDescPrefix = $"[{PaymentPurpose.AuctionBuyNow}]";
            transaction = await _dbContext.Set<Transaction>()
                .FirstOrDefaultAsync(t =>
                    t.UserId == _currentUser.UserId &&
                    t.Status == TransactionStatus.Pending &&
                    t.Type == TransactionType.Payment &&
                    t.Description != null && t.Description.StartsWith(buyNowDescPrefix) &&
                    t.Description.Contains(request.BuyNowReservationId.Value.ToString()),
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
                orderId: request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null);

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

        // 6. Tạo URL thanh toán VNPay
        var amountVnd = (long)request.Amount;

        var urlResult = _paymentGateway.CreatePaymentUrl(new CreatePaymentUrlRequest
        {
            TransactionRef = txnRef,
            Amount = amountVnd,
            OrderDescription = request.Description,
            Purpose = request.Purpose,
            IpAddress = request.IpAddress.ToString(),
            BankCode = request.BankCode,
        });

        if (urlResult.IsFailure)
            return urlResult.Error;

        return new CreateVnPayPaymentUrlResponse(
            TransactionId: transaction.Id.Value,
            TransactionRef: txnRef,
            PaymentUrl: urlResult.Value.PaymentUrl);
    }
}
