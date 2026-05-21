using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetPaymentSummary;

public sealed record GetPaymentSummaryQuery(DateTime? From, DateTime? To) : IQuery<PaymentSummaryDto>;

internal sealed class GetPaymentSummaryQueryHandler
    : IQueryHandler<GetPaymentSummaryQuery, PaymentSummaryDto>
{
    private readonly IDbContext _dbContext;

    public GetPaymentSummaryQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentSummaryDto, Error>> Handle(
        GetPaymentSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var transactions = _dbContext.Set<Transaction>().AsNoTracking().AsQueryable();
        var withdrawals = _dbContext.Set<WithdrawalRequest>().AsNoTracking().AsQueryable();
        var escrows = _dbContext.Set<Escrow>().AsNoTracking().AsQueryable();

        if (request.From.HasValue)
        {
            var from = request.From.Value;
            transactions = transactions.Where(x => (x.ProcessedAt ?? x.CreatedAt) >= from);
            withdrawals = withdrawals.Where(x => x.CreatedAt >= from);
            escrows = escrows.Where(x => x.HeldAt >= from || (x.ReleasedAt.HasValue && x.ReleasedAt.Value >= from));
        }

        if (request.To.HasValue)
        {
            var to = request.To.Value;
            transactions = transactions.Where(x => (x.ProcessedAt ?? x.CreatedAt) <= to);
            withdrawals = withdrawals.Where(x => x.CreatedAt <= to);
            escrows = escrows.Where(x => x.HeldAt <= to || (x.ReleasedAt.HasValue && x.ReleasedAt.Value <= to));
        }

        var completedPayments = await transactions.CountAsync(
            x => x.Type == TransactionType.Payment && x.Status == TransactionStatus.Completed,
            cancellationToken);

        var failedPayments = await transactions.CountAsync(
            x => x.Type == TransactionType.Payment && x.Status == TransactionStatus.Failed,
            cancellationToken);

        var walletTopUps = await transactions.CountAsync(
            x => x.Type == TransactionType.Deposit &&
                 x.Description != null &&
                 x.Description.Contains("[WalletTopUp]"),
            cancellationToken);

        var withdrawalPendingCount = await withdrawals.CountAsync(
            x => x.Status == WithdrawalStatus.Pending,
            cancellationToken);

        var withdrawalPendingTotal = await withdrawals
            .Where(x => x.Status == WithdrawalStatus.Pending)
            .Select(x => (decimal?)x.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var holdingEscrowCount = await escrows.CountAsync(
            x => x.Status == EscrowStatus.Holding,
            cancellationToken);

        var holdingEscrowTotal = await escrows
            .Where(x => x.Status == EscrowStatus.Holding)
            .Select(x => (decimal?)x.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var releasedEscrowTotal = await escrows
            .Where(x => x.Status == EscrowStatus.ReleasedToSeller)
            .Select(x => (decimal?)x.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var refundedEscrowTotal = await escrows
            .Where(x => x.Status == EscrowStatus.RefundedToBuyer)
            .Select(x => (decimal?)x.Amount.Amount)
            .SumAsync(cancellationToken) ?? 0m;

        var platformWallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Type == WalletType.Platform, cancellationToken);
            
        var totalRevenue = platformWallet != null 
            ? platformWallet.WalletFunds.BalanceAmount + platformWallet.WalletFunds.PendingBalanceAmount 
            : 0m;

        var allWalletsBalance = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .Select(x => (decimal?)(x.WalletFunds.BalanceAmount + x.WalletFunds.PendingBalanceAmount))
            .SumAsync(cancellationToken) ?? 0m;
        
        var totalSystemBalance = allWalletsBalance + holdingEscrowTotal;

        return new PaymentSummaryDto(
            CompletedPayments: completedPayments,
            FailedPayments: failedPayments,
            WalletTopUps: walletTopUps,
            WithdrawalPendingCount: withdrawalPendingCount,
            WithdrawalPendingTotal: withdrawalPendingTotal,
            HoldingEscrowCount: holdingEscrowCount,
            HoldingEscrowTotal: holdingEscrowTotal,
            ReleasedEscrowTotal: releasedEscrowTotal,
            RefundedEscrowTotal: refundedEscrowTotal,
            TotalRevenue: totalRevenue,
            TotalSystemBalance: totalSystemBalance);
    }
}
