using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record DeletePaymentMethodCommand(Guid PaymentMethodId) : ICommand;

internal sealed class DeletePaymentMethodCommandHandler : ICommandHandler<DeletePaymentMethodCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ILogger<DeletePaymentMethodCommandHandler> _logger;

    public DeletePaymentMethodCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPaymentGatewayService paymentGateway,
        ILogger<DeletePaymentMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethodId = PaymentMethodId.From(request.PaymentMethodId);

        var targetMethod = await _dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.Id == paymentMethodId && p.UserId == _currentUser.UserId, cancellationToken);

        if (targetMethod is null)
            return Error.NotFound("PaymentMethod.NotFound", "Payment method not found.");

        if (!targetMethod.IsActive)
            return UnitResult.Success<Error>();

        // Nếu là VNPay token → xóa token phía VNPay (best-effort)
        if (targetMethod.Type == PaymentMethodType.VnPay &&
            !string.IsNullOrWhiteSpace(targetMethod.VnPayToken))
        {
            try
            {
                var txnRef = $"DEL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..36];
                var removeResult = await _paymentGateway.RemoveTokenAsync(new RemoveTokenRequest
                {
                    AppUserId = _currentUser.UserId.Value.ToString(),
                    Token = targetMethod.VnPayToken,
                    TransactionRef = txnRef,
                    Description = $"Remove payment method {paymentMethodId.Value}",
                    IpAddress = "127.0.0.1",
                }, cancellationToken);

                if (removeResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Failed to remove VNPay token for PaymentMethodId={Id}: {Error}",
                        paymentMethodId.Value, removeResult.Error.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Exception removing VNPay token for PaymentMethodId={Id}",
                    paymentMethodId.Value);
            }
        }

        targetMethod.Deactivate();
        _dbContext.Update(targetMethod);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
