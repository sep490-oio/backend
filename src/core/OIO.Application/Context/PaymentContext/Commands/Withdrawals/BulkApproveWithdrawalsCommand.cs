using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Commands.Withdrawals;

public sealed record BulkApproveWithdrawalsCommand(List<Guid> WithdrawalIds) : ICommand;

internal sealed class BulkApproveWithdrawalsCommandHandler
    : ICommandHandler<BulkApproveWithdrawalsCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public BulkApproveWithdrawalsCommandHandler(
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
        BulkApproveWithdrawalsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WithdrawalIds is not { Count: > 0 })
            return Error.Validation("ids", "Withdrawal.EmptyBatch", "At least one withdrawal ID is required.");

        var now = _clock.UtcNow;
        var adminId = _currentUser.UserId;

        var ids = request.WithdrawalIds
            .Distinct()
            .Select(WithdrawalRequestId.From)
            .ToList();

        // Load all requested withdrawals in a single query.
        var withdrawals = await _dbContext.Set<WithdrawalRequest>()
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(cancellationToken);

        if (withdrawals.Count != ids.Count)
        {
            var foundIds = withdrawals.Select(w => w.Id.Value).ToHashSet();
            var missing = ids.Where(id => !foundIds.Contains(id.Value)).Select(id => id.Value).ToList();
            return Error.NotFound("Withdrawal.NotFound",
                $"Withdrawal(s) not found: {string.Join(", ", missing)}");
        }

        // Begin explicit transaction — all-or-nothing.
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var withdrawal in withdrawals)
            {
                var approveResult = withdrawal.Approve(adminId, now);
                if (approveResult.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Error.Conflict("Withdrawal.BulkApproveFailed",
                        $"Failed to approve withdrawal {withdrawal.Id.Value}: {approveResult.Error.Message}");
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return UnitResult.Success<Error>();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
