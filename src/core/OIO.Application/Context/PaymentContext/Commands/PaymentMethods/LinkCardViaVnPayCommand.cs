using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

/// <summary>
/// Tạo URL VNPay token_create để user link thẻ (không thanh toán).
/// User redirect → nhập thẻ → OTP → callback tạo PaymentMethod tự động.
/// </summary>
public sealed record LinkCardViaVnPayCommand(
    string? CardType = null) : ICommand<LinkCardViaVnPayResponse>;

public sealed record LinkCardViaVnPayResponse(
    string RedirectUrl,
    string TransactionRef);

internal sealed class LinkCardViaVnPayCommandHandler
    : ICommandHandler<LinkCardViaVnPayCommand, LinkCardViaVnPayResponse>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public LinkCardViaVnPayCommandHandler(
        IPaymentGatewayService paymentGateway,
        ICurrentUser currentUser,
        IClock clock)
    {
        _paymentGateway = paymentGateway;
        _currentUser = currentUser;
        _clock = clock;
    }

    public Task<Result<LinkCardViaVnPayResponse, Error>> Handle(
        LinkCardViaVnPayCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var txnRef = $"LINK-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36];

        var urlResult = _paymentGateway.CreateTokenOnlyUrl(new CreateTokenPaymentUrlRequest
        {
            TransactionRef = txnRef,
            Amount = 0,
            OrderDescription = "Lien ket the thanh toan",
            AppUserId = _currentUser.UserId.Value.ToString(),
            IpAddress = "127.0.0.1",
            CardType = request.CardType,
        });

        if (urlResult.IsFailure)
            return Task.FromResult(Result.Failure<LinkCardViaVnPayResponse, Error>(urlResult.Error));

        var response = new LinkCardViaVnPayResponse(
            RedirectUrl: urlResult.Value.PaymentUrl,
            TransactionRef: urlResult.Value.TransactionRef);

        return Task.FromResult(Result.Success<LinkCardViaVnPayResponse, Error>(response));
    }
}
