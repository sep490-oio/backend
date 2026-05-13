using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Withdrawals;

// ─── Create Withdrawal Request ───────────────────────────────────────

public sealed record CreateWithdrawalRequestCommand(
    decimal Amount,
    string BankName,
    string AccountNumber,
    string AccountHolder) : ICommand<CreateWithdrawalRequestResponse>;

public sealed record CreateWithdrawalRequestResponse(
    Guid WithdrawalRequestId,
    decimal Amount,
    decimal Fee,
    decimal NetAmount,
    string Status);

internal sealed class CreateWithdrawalRequestCommandHandler
    : ICommandHandler<CreateWithdrawalRequestCommand, CreateWithdrawalRequestResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public CreateWithdrawalRequestCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateWithdrawalRequestResponse, Error>> Handle(
        CreateWithdrawalRequestCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        // 1. Lấy wallet của user
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.IsActive, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Active wallet not found.");

        // 2. Tính phí rút tiền (có thể cấu hình, hiện tại 0)
        decimal fee = 0;

        // 3. Tạo Transaction (Pending) để ghi nhận withdrawal vào sổ cái
        var txNumberResult = TransactionNumber.Create($"WD-{Guid.CreateVersion7():N}");
        if (txNumberResult.IsFailure)
            return txNumberResult.Error;

        var amountMoneyResult = Money.Create(request.Amount, wallet.WalletFunds.Currency);
        if (amountMoneyResult.IsFailure)
            return amountMoneyResult.Error;

        var txResult = Transaction.Create(
            userId,
            txNumberResult.Value,
            TransactionType.Withdrawal,
            amountMoneyResult.Value,
            wallet.WalletFunds.Currency.Id,
            $"Withdrawal request - {request.Amount:N0} to {request.BankName} / {request.AccountHolder}",
            now);

        if (txResult.IsFailure)
            return txResult.Error;

        var transaction = txResult.Value;
        if (fee > 0)
        {
            var netMoneyResult = Money.Create(request.Amount - fee, wallet.WalletFunds.Currency);
            if (netMoneyResult.IsSuccess)
                transaction.SetFee(fee, netMoneyResult.Value);
        }
        _dbContext.Set<Transaction>().Add(transaction);

        // 4. Hold tiền trong ví (liên kết transactionId)
        var holdResult = wallet.Hold(
            request.Amount,
            transactionId: transaction.Id,
            description: $"Withdrawal hold - {request.Amount}",
            nowUtc: now);

        if (holdResult.IsFailure)
            return holdResult.Error;

        // 5. Tạo WithdrawalRequest
        var bankAccount = BankAccount.Create(
            request.BankName,
            request.AccountNumber,
            request.AccountHolder);

        var withdrawal = WithdrawalRequest.Create(
            userId,
            wallet.Id,
            request.Amount,
            fee,
            bankAccount,
            now);

        _dbContext.Set<WithdrawalRequest>().Add(withdrawal);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateWithdrawalRequestResponse(
            withdrawal.Id.Value,
            withdrawal.Amount,
            withdrawal.Fee,
            withdrawal.NetAmount,
            withdrawal.Status.Id);
    }
}
