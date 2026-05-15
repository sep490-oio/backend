using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Withdrawals;

public sealed record CancelWithdrawalRequestCommand(Guid WithdrawalRequestId) : ICommand<WithdrawalRequestDto>;

internal sealed class CancelWithdrawalRequestCommandHandler
    : ICommandHandler<CancelWithdrawalRequestCommand, WithdrawalRequestDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public CancelWithdrawalRequestCommandHandler(
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

    public async Task<Result<WithdrawalRequestDto, Error>> Handle(
        CancelWithdrawalRequestCommand request,
        CancellationToken cancellationToken)
    {
        var withdrawalRequestId = WithdrawalRequestId.From(request.WithdrawalRequestId);

        var withdrawal = await _dbContext.Set<WithdrawalRequest>()
            .FirstOrDefaultAsync(
                x => x.Id == withdrawalRequestId && x.UserId == _currentUser.UserId,
                cancellationToken);

        if (withdrawal is null)
            return Error.NotFound("Withdrawal.NotFound", "Withdrawal request not found.");

        var cancelResult = withdrawal.Cancel(_clock.UtcNow);
        if (cancelResult.IsFailure)
            return cancelResult.Error;

        var wallet = await _dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(x => x.Id == withdrawal.WalletId, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Wallet not found.");

        var unholdResult = wallet.Unhold(
            withdrawal.Amount,
            transactionId: null,
            description: $"Withdrawal cancelled - unheld {withdrawal.Amount}",
            nowUtc: _clock.UtcNow);

        if (unholdResult.IsFailure)
            return Error.Conflict("Wallet.UnholdFailed", unholdResult.Error.Message);

        // Mark corresponding Transaction as Failed so it doesn't stay pending for admins
        var txn = await _dbContext.Set<Transaction>()
            .FirstOrDefaultAsync(t =>
                t.UserId == withdrawal.UserId &&
                t.Type == OIO.Domain.Context.PaymentContext.Enums.TransactionType.Withdrawal &&
                (t.Status == OIO.Domain.Context.PaymentContext.Enums.TransactionStatus.Pending || t.Status == OIO.Domain.Context.PaymentContext.Enums.TransactionStatus.Processing) &&
                t.Amount.Amount == withdrawal.Amount,
                cancellationToken);

        if (txn is not null)
        {
            txn.MarkAsFailed(OIO.Domain.Context.PaymentContext.ValueObjects.GatewayInfo.Empty, _clock.UtcNow);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PaymentReadModelMapper.ToDto(withdrawal);
    }
}
