using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.ReconcileTransactions;

public sealed record ProcessGatewayReconciliationCommand(int BatchSize = 50) : ICommand;

internal sealed class ProcessGatewayReconciliationCommandHandler : ICommandHandler<ProcessGatewayReconciliationCommand>
{
    private readonly IPaymentGatewayService _paymentGateway;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ProcessGatewayReconciliationCommandHandler> _logger;

    public ProcessGatewayReconciliationCommandHandler(
        IPaymentGatewayService paymentGateway,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ProcessGatewayReconciliationCommandHandler> logger)
    {
        _paymentGateway = paymentGateway;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> Handle(ProcessGatewayReconciliationCommand request, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var thresholdDate = now.AddMinutes(-15); // Chỉ quét giao dịch treo quá 15 phút

        var pendingTransactions = await _dbContext.Set<Transaction>()
            .Where(t => t.Status == TransactionStatus.Pending 
                        && t.CreatedAt <= thresholdDate
                        && t.Gateway != null 
                        && t.Gateway.Provider == _paymentGateway.ProviderCode)
            .OrderBy(t => t.CreatedAt)
            .Take(request.BatchSize)
            .ToListAsync(cancellationToken);

        if (pendingTransactions.Count == 0)
            return UnitResult.Success<Error>();

        foreach (var transaction in pendingTransactions)
        {
            try
            {
                var createdDateStr = transaction.CreatedAt.AddHours(7).ToString("yyyyMMddHHmmss"); // VNPay use GMT+7
                var queryResult = await _paymentGateway.QueryTransactionAsync(transaction.TransactionNumber.Value, createdDateStr, cancellationToken);
                
                if (queryResult.IsFailure)
                {
                    _logger.LogWarning("Reconciliation failed for TxnRef={TxnRef}: {Error}", transaction.TransactionNumber.Value, queryResult.Error.Message);
                    continue;
                }

                var vnPayResult = queryResult.Value;

                // Nếu VNPay chưa có thông tin (01 - chưa thanh toán, hoặc lỗi không tìm thấy), 
                // sau một thời gian dài (vd 24h) ta có thể mark là Failed. Ở đây tạm để đơn giản:
                if (vnPayResult.ResponseCode == "00" && vnPayResult.TransactionStatus == "00")
                {
                    var gatewayInfo = GatewayInfo.Create(
                        provider: _paymentGateway.ProviderCode,
                        transactionId: "", // we might not get vnp_TransactionNo in query response depending on VNPay, but usually we do
                        response: vnPayResult.RawResponseJson);

                    transaction.MarkAsCompleted(gatewayInfo, now);
                    _logger.LogInformation("Reconciliation: TxnRef={TxnRef} marked as Completed", transaction.TransactionNumber.Value);
                    
                    // Note: Nếu ta muốn trigger các logic như Escrow/Wallet khi query thành công, 
                    // ta cần dispatch Domain Event thay vì gọi trực tiếp ở đây, 
                    // hoặc refactor logic xử lý webhook thành chung. 
                    // Nhưng tạm thời ta cập nhật status Transaction trước.
                }
                else if (vnPayResult.TransactionStatus != "01" && vnPayResult.TransactionStatus != "02") 
                {
                    // 01: chưa thanh toán, 02: đang giao dịch. Khác 00, 01, 02 coi như thất bại
                    var gatewayInfo = GatewayInfo.Create(
                        provider: _paymentGateway.ProviderCode,
                        transactionId: "",
                        response: vnPayResult.RawResponseJson);

                    transaction.MarkAsFailed(gatewayInfo, now);
                    _logger.LogInformation("Reconciliation: TxnRef={TxnRef} marked as Failed with Status={Status}", transaction.TransactionNumber.Value, vnPayResult.TransactionStatus);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconciling TxnRef={TxnRef}", transaction.TransactionNumber.Value);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
