using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.RefundVnPayTransaction;

/// <summary>
/// Command hoàn tiền qua VNPay.
/// </summary>
public sealed record RefundVnPayTransactionCommand(
    string OriginalTransactionRef,
    string OriginalVnPayTransactionNo,
    decimal Amount,
    string Reason,
    IPAddress IpAddress) : ICommand<RefundVnPayTransactionResponse>;

public sealed record RefundVnPayTransactionResponse(
    bool IsSuccess,
    string ResponseCode,
    string? Message);

internal sealed class RefundVnPayTransactionCommandHandler
    : ICommandHandler<RefundVnPayTransactionCommand, RefundVnPayTransactionResponse>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<RefundVnPayTransactionCommandHandler> _logger;

    public RefundVnPayTransactionCommandHandler(
        IPaymentGatewayService paymentGateway,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        ILogger<RefundVnPayTransactionCommandHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<RefundVnPayTransactionResponse, Error>> Handle(
        RefundVnPayTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        // 1. Tìm Transaction gốc trong DB
        var transaction = await _dbContext.Set<Transaction>()
            .FirstOrDefaultAsync(
                t => t.TransactionNumber.Value == request.OriginalTransactionRef,
                cancellationToken);

        if (transaction is null)
        {
            return Error.NotFound("Transaction.NotFound",
                $"Transaction with ref '{request.OriginalTransactionRef}' not found.");
        }

        // 2. Gọi VNPay refund API
        var refundResult = await _paymentGateway.RefundAsync(new RefundRequest
        {
            OriginalTransactionRef = request.OriginalTransactionRef,
            OriginalVnPayTransactionNo = request.OriginalVnPayTransactionNo,
            Amount = (long)request.Amount,
            Reason = request.Reason,
            IpAddress = request.IpAddress.ToString(),
            CreatedBy = _currentUser.UserName?.Value ?? _currentUser.UserId.Value.ToString(),
        }, cancellationToken);

        if (refundResult.IsFailure)
            return refundResult.Error;

        var refund = refundResult.Value;

        // 3. Cập nhật Transaction status → Refunded nếu thành công
        if (refund.IsSuccess)
        {
            var markResult = transaction.MarkAsRefunded(now);
            if (markResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to mark transaction as refunded: {Error}",
                    markResult.Error.Message);
            }
            else
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        _logger.LogInformation(
            "VNPay refund result for TxnRef={TxnRef}: Success={IsSuccess}, ResponseCode={ResponseCode}",
            request.OriginalTransactionRef, refund.IsSuccess, refund.ResponseCode);

        return new RefundVnPayTransactionResponse(
            IsSuccess: refund.IsSuccess,
            ResponseCode: refund.ResponseCode,
            Message: refund.Message);
    }
}
