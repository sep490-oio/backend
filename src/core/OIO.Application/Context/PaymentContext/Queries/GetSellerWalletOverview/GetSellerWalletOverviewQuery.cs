using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetSellerWalletOverview;

public sealed record GetSellerWalletOverviewQuery() : IQuery<SellerWalletOverviewDto>;

internal sealed class GetSellerWalletOverviewQueryHandler
    : IQueryHandler<GetSellerWalletOverviewQuery, SellerWalletOverviewDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetSellerWalletOverviewQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<SellerWalletOverviewDto, Error>> Handle(
        GetSellerWalletOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var pendingWithdrawalAmount = await _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == WithdrawalStatus.Pending)
            .SumAsync(w => (decimal?)w.Amount, cancellationToken) ?? 0m;

        var escrowHoldingAmount = await _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(e => e.Order)
            .Where(e => e.Order.SellerId == userId && e.Status == EscrowStatus.Holding)
            .SumAsync(e => (decimal?)e.Amount.Amount, cancellationToken) ?? 0m;

        var releasedToWalletAmount = await _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(e => e.Order)
            .Where(e => e.Order.SellerId == userId && e.Status == EscrowStatus.ReleasedToSeller)
            .SumAsync(e => (decimal?)e.Amount.Amount, cancellationToken) ?? 0m;

        if (wallet is null)
        {
            return new SellerWalletOverviewDto(
                AvailableBalance: 0m,
                PendingWithdrawalAmount: pendingWithdrawalAmount,
                EscrowHoldingAmount: escrowHoldingAmount,
                ReleasedToWalletAmount: releasedToWalletAmount,
                Currency: "VND",
                UpdatedAt: DateTime.UtcNow);
        }

        return new SellerWalletOverviewDto(
            AvailableBalance: wallet.WalletFunds.BalanceAmount,
            PendingWithdrawalAmount: pendingWithdrawalAmount,
            EscrowHoldingAmount: escrowHoldingAmount,
            ReleasedToWalletAmount: releasedToWalletAmount,
            Currency: wallet.WalletFunds.Currency.Id,
            UpdatedAt: wallet.ModifiedAt ?? wallet.CreatedAt);
    }
}
