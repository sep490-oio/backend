using System.Text.Json;
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.Context.PaymentContext.Aggregates.Webhooks;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.SaveVnPayWebhook;

public sealed record SaveVnPayWebhookCommand(IDictionary<string, string> QueryParams) : ICommand;

internal sealed class SaveVnPayWebhookCommandHandler : ICommandHandler<SaveVnPayWebhookCommand>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SaveVnPayWebhookCommandHandler(
        IPaymentGatewayService paymentGateway,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _paymentGateway = paymentGateway;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(SaveVnPayWebhookCommand request, CancellationToken cancellationToken)
    {
        // Thử parse callback bằng service để tận dụng logic validate chữ ký
        var callbackResult = _paymentGateway.ProcessCallback(request.QueryParams);
        if (callbackResult.IsFailure)
            return callbackResult.Error;

        var rawContent = JsonSerializer.Serialize(request.QueryParams);

        // Tạo webhook event để xử lý bất đồng bộ
        var webhookEvent = GatewayWebhookEvent.Create(
            provider: "vnpay",
            eventType: "ipn",
            rawContent: rawContent,
            nowUtc: _clock.UtcNow);

        _dbContext.Insert(webhookEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
