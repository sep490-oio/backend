using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.PaymentMethods;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.PaymentMethods;

public sealed record HardDeletePaymentMethodCommand(Guid PaymentMethodId) : ICommand;

internal sealed class HardDeletePaymentMethodCommandHandler : ICommandHandler<HardDeletePaymentMethodCommand>
{
    private const int MaxVnPayRemoveAttempts = 3;

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly ILogger<HardDeletePaymentMethodCommandHandler> _logger;

    public HardDeletePaymentMethodCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IPaymentGatewayService paymentGateway,
        ILogger<HardDeletePaymentMethodCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(HardDeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethodId = PaymentMethodId.From(request.PaymentMethodId);

        var targetMethod = await _dbContext.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.Id == paymentMethodId && p.UserId == _currentUser.UserId, cancellationToken);

        if (targetMethod is null)
            return Error.NotFound("PaymentMethod.NotFound", "Payment method not found.");

        if (targetMethod.IsDefault)
            return Error.Conflict(
                "PaymentMethod.IsDefault",
                "Cannot hard-delete a default payment method. Set another method as default first.");

        var pendingStatus = TransactionStatus.Pending;
        var processingStatus = TransactionStatus.Processing;
        var hasInFlight = await _dbContext.Set<Transaction>()
            .AnyAsync(
                t => t.PaymentMethodId == paymentMethodId
                     && (t.Status == pendingStatus || t.Status == processingStatus),
                cancellationToken);

        if (hasInFlight)
            return Error.Conflict(
                "PaymentMethod.TransactionsInFlight",
                "Cannot hard-delete while transactions are pending.");

        // VNPay token cleanup — hybrid 3-retry with exponential backoff (0ms, 300ms, 600ms).
        if (targetMethod.Type == PaymentMethodType.VnPay &&
            !string.IsNullOrWhiteSpace(targetMethod.VnPayToken))
        {
            Error? lastError = null;
            for (var attempt = 1; attempt <= MaxVnPayRemoveAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    // attempt 1 -> 0ms, attempt 2 -> 300ms, attempt 3 -> 600ms
                    await Task.Delay(300 * (1 << (attempt - 2)), cancellationToken);
                }

                try
                {
                    var txnRef = $"HDEL-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
                    if (txnRef.Length > 36)
                        txnRef = txnRef[..36];

                    var result = await _paymentGateway.RemoveTokenAsync(new RemoveTokenRequest
                    {
                        AppUserId = _currentUser.UserId.Value.ToString(),
                        Token = targetMethod.VnPayToken!,
                        TransactionRef = txnRef,
                        Description = $"Hard-delete payment method {paymentMethodId.Value}",
                        IpAddress = "127.0.0.1",
                    }, cancellationToken);

                    if (result.IsSuccess)
                    {
                        lastError = null;
                        break;
                    }

                    lastError = result.Error;
                    _logger.LogWarning(
                        "VnPay RemoveToken attempt {Attempt} failed for PaymentMethodId={Id}: {Error}",
                        attempt, paymentMethodId.Value, result.Error.Message);
                }
                catch (Exception ex)
                {
                    lastError = Error.Unavailable("PaymentMethod.VnPayRemovalFailed", ex.Message);
                    _logger.LogWarning(ex,
                        "VnPay RemoveToken attempt {Attempt} threw for PaymentMethodId={Id}",
                        attempt, paymentMethodId.Value);
                }
            }

            if (lastError is not null)
            {
                return Error.Unavailable(
                    "PaymentMethod.VnPayRemovalFailed",
                    $"VNPay token removal failed after {MaxVnPayRemoveAttempts} attempts.");
            }
        }

        _dbContext.Set<PaymentMethod>().Remove(targetMethod);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
