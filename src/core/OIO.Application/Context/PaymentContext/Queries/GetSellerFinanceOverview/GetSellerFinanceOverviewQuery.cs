using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Aggregates.Withdrawals;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.GetSellerFinanceOverview;

public sealed record GetSellerFinanceOverviewQuery() : IQuery<SellerFinanceOverviewDto>;

internal sealed class GetSellerFinanceOverviewQueryHandler
    : IQueryHandler<GetSellerFinanceOverviewQuery, SellerFinanceOverviewDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;

    public GetSellerFinanceOverviewQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<SellerFinanceOverviewDto, Error>> Handle(
        GetSellerFinanceOverviewQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var settlementOptions = _runtimeSettings.Settlement;

        // Wallet (balance + pending). Single-currency assumption mirrors
        // existing GetSellerWalletOverview — VND fallback when no wallet yet.
        var wallet = await _dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        var withdrawableBalance = wallet?.WalletFunds.BalanceAmount ?? 0m;
        var currency = wallet?.WalletFunds.Currency.Id ?? "VND";
        var updatedAt = wallet?.ModifiedAt ?? wallet?.CreatedAt ?? DateTime.UtcNow;

        // Pending withdrawal amount (already reserved against wallet).
        var pendingWithdrawalAmount = await _dbContext.Set<WithdrawalRequest>()
            .AsNoTracking()
            .Where(w => w.UserId == userId && w.Status == WithdrawalStatus.Pending)
            .SumAsync(w => (decimal?)w.Amount, cancellationToken) ?? 0m;

        // All Holding escrows for this seller — joined with their order so we
        // can split by order status / dispute flag and feed the fee calculator.
        var holdingEscrows = await _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(e => e.Order)
            .Where(e => e.Order.SellerId == userId && e.Status == EscrowStatus.Holding)
            .Select(e => new
            {
                e.Amount.Amount,
                Currency = e.Currency,
                OrderStatus = e.Order.Status,
                e.Order.DisputedAt,
                e.Order.IsPlatformVerifiedItem
            })
            .ToListAsync(cancellationToken);

        var grossEscrowHolding = holdingEscrows.Sum(x => x.Amount);
        var disputedEscrowAmount = holdingEscrows
            .Where(x => x.DisputedAt != null)
            .Sum(x => x.Amount);
        var readyToReleaseAmount = holdingEscrows
            .Where(x => x.DisputedAt == null
                        && (x.OrderStatus == OrderStatus.Delivered
                            || x.OrderStatus == OrderStatus.Completed))
            .Sum(x => x.Amount);

        // Estimated breakdown across non-disputed Holding escrows. Reuses
        // EscrowSettlementService.CalculateSellerSettlement — no duplication.
        var estimatedSellerNetPayout = 0m;
        var estimatedPlatformCommission = 0m;
        var estimatedInspectionFee = 0m;

        foreach (var escrow in holdingEscrows.Where(x => x.DisputedAt == null))
        {
            var settlementResult = EscrowSettlementService.CalculateSellerSettlement(
                settlementOptions,
                escrow.Amount,
                escrow.Currency,
                includeInspectionFee: escrow.IsPlatformVerifiedItem);

            if (settlementResult.IsFailure)
                return settlementResult.Error;

            var breakdown = settlementResult.Value;
            estimatedSellerNetPayout += breakdown.SellerNetAmount;
            estimatedPlatformCommission += breakdown.PlatformCommission;
            estimatedInspectionFee += breakdown.InspectionFee;
        }

        // Future-charged seller fees still pending (e.g. inspection fee
        // recorded as a Pending Fee transaction during a buyer-win dispute
        // when the seller wallet had insufficient balance).
        var pendingSellerFeeCharges = await _dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.UserId == userId
                        && t.Type == TransactionType.Fee
                        && t.Status == TransactionStatus.Pending)
            .SumAsync(t => (decimal?)t.Amount.Amount, cancellationToken) ?? 0m;

        return new SellerFinanceOverviewDto(
            WithdrawableBalance: withdrawableBalance,
            PendingWithdrawalAmount: pendingWithdrawalAmount,
            GrossEscrowHolding: grossEscrowHolding,
            ReadyToReleaseAmount: readyToReleaseAmount,
            DisputedEscrowAmount: disputedEscrowAmount,
            EstimatedSellerNetPayout: estimatedSellerNetPayout,
            EstimatedPlatformCommission: estimatedPlatformCommission,
            EstimatedInspectionFee: estimatedInspectionFee,
            PendingSellerFeeCharges: pendingSellerFeeCharges,
            Currency: currency,
            UpdatedAt: updatedAt);
    }
}
