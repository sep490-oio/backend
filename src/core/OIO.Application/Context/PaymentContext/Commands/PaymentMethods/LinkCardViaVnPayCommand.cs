using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

/// <summary>
/// Tạo URL VNPay token_create để user link thẻ (không thanh toán).
/// User redirect → nhập thẻ → OTP → callback tạo PaymentMethod tự động.
///
/// CardType follows VNPay token spec codes:
///   "01" = ATM domestic card (default)
///   "02" = international credit/debit card
/// Do not pass gateway-pay values like "vnpay" here.
/// </summary>
public sealed record LinkCardViaVnPayCommand(
    string? CardType = null) : ICommand<LinkCardViaVnPayResponse>;

internal static class LinkCardViaVnPayCardTypes
{
    public const string DomesticAtm = "01";
    public const string InternationalCard = "02";

    /// <summary>
    /// Resolves user-supplied card type to a spec-compliant code.
    /// Rejects legacy values like "vnpay" by falling back to the domestic default.
    /// </summary>
    public static string Resolve(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return DomesticAtm;
        var trimmed = raw.Trim();
        return trimmed is DomesticAtm or InternationalCard ? trimmed : DomesticAtm;
    }
}

public sealed record LinkCardViaVnPayResponse(
    string RedirectUrl,
    string TransactionRef);

internal sealed class LinkCardViaVnPayCommandHandler
    : ICommandHandler<LinkCardViaVnPayCommand, LinkCardViaVnPayResponse>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public LinkCardViaVnPayCommandHandler(
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

    public async Task<Result<LinkCardViaVnPayResponse, Error>> Handle(
        LinkCardViaVnPayCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var txnRef = $"LINK-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36];

        // Create a Transaction record so the callback handler can find it by txnRef
        var (_, isTxnNumFailure, txnNumber, txnNumError) = TransactionNumber.Create(txnRef);
        if (isTxnNumFailure)
            return txnNumError;

        var (_, isMoneyFailure, money, moneyError) = Money.Create(0, "VND");
        if (isMoneyFailure)
            return moneyError;

        var (_, isTxnFailure, transaction, txnError) = Transaction.Create(
            userId: _currentUser.UserId,
            transactionNumber: txnNumber,
            type: TransactionType.Payment, // reuse Payment type for card-link
            amount: money,
            currency: "VND",
            description: LedgerDescriptions.LinkPaymentCard(),
            nowUtc: now);

        if (isTxnFailure)
            return txnError;

        _dbContext.Insert(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate VnPay token_create URL. CardType must be spec code (01/02).
        var urlResult = _paymentGateway.CreateTokenOnlyUrl(new CreateTokenPaymentUrlRequest
        {
            TransactionRef = txnRef,
            Amount = 0,
            OrderDescription = "Lien ket the thanh toan",
            AppUserId = _currentUser.UserId.Value.ToString(),
            IpAddress = "127.0.0.1",
            CardType = LinkCardViaVnPayCardTypes.Resolve(request.CardType),
        });

        if (urlResult.IsFailure)
            return urlResult.Error;

        var response = new LinkCardViaVnPayResponse(
            RedirectUrl: urlResult.Value.PaymentUrl,
            TransactionRef: urlResult.Value.TransactionRef);

        return response;
    }
}
