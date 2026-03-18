using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Withdrawals;

// ─── Approve Withdrawal ──────────────────────────────────────────────

public sealed record ApproveWithdrawalCommand(Guid WithdrawalRequestId) : ICommand;

internal sealed class ApproveWithdrawalCommandHandler
    : ICommandHandler<ApproveWithdrawalCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public ApproveWithdrawalCommandHandler(
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

    public async Task<UnitResult<Error>> Handle(
        ApproveWithdrawalCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var adminId = _currentUser.UserId;
        var withdrawalRequestId = WithdrawalRequestId.From(request.WithdrawalRequestId);

        var withdrawal = await _dbContext.Set<WithdrawalRequest>()
            .FirstOrDefaultAsync(w => w.Id == withdrawalRequestId, cancellationToken);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", $"Withdrawal request {request.WithdrawalRequestId} not found.");

        var approveResult = withdrawal.Approve(adminId, now);
        if (approveResult.IsFailure)
            return approveResult.Error;

        // Sau khi Approve, tiền vẫn ở trạng thái Hold.
        // Job hoặc Admin sẽ trigger MarkAsProcessing -> MarkAsCompleted -> Wallet.DebitPending.

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}

// ─── Reject Withdrawal ───────────────────────────────────────────────

public sealed record RejectWithdrawalCommand(Guid WithdrawalRequestId, string Reason) : ICommand;

internal sealed class RejectWithdrawalCommandHandler
    : ICommandHandler<RejectWithdrawalCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public RejectWithdrawalCommandHandler(
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

    public async Task<UnitResult<Error>> Handle(
        RejectWithdrawalCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var adminId = _currentUser.UserId;
        var withdrawalRequestId = WithdrawalRequestId.From(request.WithdrawalRequestId);

        var withdrawal = await _dbContext.Set<WithdrawalRequest>()
            .FirstOrDefaultAsync(w => w.Id == withdrawalRequestId, cancellationToken);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", $"Withdrawal request {request.WithdrawalRequestId} not found.");

        // Reject yêu cầu
        var rejectResult = withdrawal.Reject(adminId, request.Reason, now);
        if (rejectResult.IsFailure)
            return rejectResult.Error;

        // Unhold tiền từ Wallet
        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.Id == withdrawal.WalletId, cancellationToken);

        if (wallet is not null)
        {
            var unholdResult = wallet.Unhold(
                withdrawal.Amount,
                transactionId: null,
                description: $"Withdrawal rejected - unheld {withdrawal.Amount}",
                nowUtc: now);

            if (unholdResult.IsFailure)
                return unholdResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
