using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformRevenueHistory;

// ── DTOs ────────────────────────────────────────────────────────────────

public sealed record PlatformRevenueDataPointDto(
    string Date,
    decimal Commission,
    decimal InspectionFees,
    decimal ForfeitIncome,
    decimal Refunds,
    decimal NetRevenue);

public sealed record PlatformRevenueHistoryDto(
    decimal TotalRevenue,
    decimal TotalCommission,
    decimal TotalInspectionFees,
    decimal TotalForfeitIncome,
    decimal TotalRefunds,
    string Currency,
    IReadOnlyList<PlatformRevenueDataPointDto> DataPoints);

// ── Query ───────────────────────────────────────────────────────────────

public sealed record GetPlatformRevenueHistoryQuery(
    DateOnly? From,
    DateOnly? To,
    string Granularity = "day") : IQuery<PlatformRevenueHistoryDto>;

// ── Handler ─────────────────────────────────────────────────────────────

internal sealed class GetPlatformRevenueHistoryQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPlatformRevenueHistoryQuery, PlatformRevenueHistoryDto>
{
    public async Task<Result<PlatformRevenueHistoryDto, Error>> Handle(
        GetPlatformRevenueHistoryQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Find the platform wallet
        var platformWallet = await dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Type == WalletType.Platform, cancellationToken);

        if (platformWallet is null)
            return Error.NotFound("PlatformWallet.NotFound", "Platform wallet not found.");

        var currency = platformWallet.WalletFunds.Currency.Id;

        // 2. Query platform wallet transactions (only credits = income)
        var query = dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Include(wt => wt.Transaction)
            .Where(wt => wt.WalletId == platformWallet.Id);

        // Date filters
        var from = request.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                   ?? DateTime.UtcNow.AddDays(-30);
        var to = request.To?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
                 ?? DateTime.UtcNow;

        query = query.Where(wt => wt.CreatedAt >= from && wt.CreatedAt <= to);

        var transactions = await query
            .OrderBy(wt => wt.CreatedAt)
            .ToListAsync(cancellationToken);

        // 3. Classify each transaction
        var classified = transactions.Select(wt =>
        {
            var category = ClassifyPlatformTransaction(wt);
            return new
            {
                wt.CreatedAt,
                wt.Amount,
                wt.Type,
                Category = category
            };
        }).ToList();

        // 4. Group by granularity
        var grouped = classified
            .GroupBy(x => GetBucketKey(x.CreatedAt, request.Granularity))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var commission = g.Where(x => x.Category == "commission").Sum(x => x.Amount);
                var inspectionFees = g.Where(x => x.Category == "inspection_fee").Sum(x => x.Amount);
                var forfeitIncome = g.Where(x => x.Category == "forfeit").Sum(x => x.Amount);
                var refunds = g.Where(x => x.Category == "refund").Sum(x => x.Amount);
                var netRevenue = commission + inspectionFees + forfeitIncome - refunds;

                return new PlatformRevenueDataPointDto(
                    Date: g.Key,
                    Commission: commission,
                    InspectionFees: inspectionFees,
                    ForfeitIncome: forfeitIncome,
                    Refunds: refunds,
                    NetRevenue: netRevenue);
            })
            .ToList();

        var totalCommission = grouped.Sum(g => g.Commission);
        var totalInspection = grouped.Sum(g => g.InspectionFees);
        var totalForfeit = grouped.Sum(g => g.ForfeitIncome);
        var totalRefunds = grouped.Sum(g => g.Refunds);

        return new PlatformRevenueHistoryDto(
            TotalRevenue: totalCommission + totalInspection + totalForfeit - totalRefunds,
            TotalCommission: totalCommission,
            TotalInspectionFees: totalInspection,
            TotalForfeitIncome: totalForfeit,
            TotalRefunds: totalRefunds,
            Currency: currency,
            DataPoints: grouped);
    }

    /// <summary>
    /// Classify platform wallet transactions by their description patterns.
    /// Platform wallet only receives credits (income) from escrow settlement.
    /// </summary>
    private static string ClassifyPlatformTransaction(WalletTransaction wt)
    {
        var desc = wt.Description ?? string.Empty;
        var txDesc = wt.Transaction?.Description ?? string.Empty;

        // Inspection fee
        if (desc.Contains(LedgerMarkers.Inspection, StringComparison.OrdinalIgnoreCase) ||
            txDesc.Contains(LedgerMarkers.Inspection, StringComparison.OrdinalIgnoreCase))
            return "inspection_fee";

        // Forfeit / penalty
        if (desc.Contains(LedgerMarkers.Forfeit, StringComparison.OrdinalIgnoreCase) ||
            desc.Contains(LedgerMarkers.Penalty, StringComparison.OrdinalIgnoreCase) ||
            txDesc.Contains(LedgerMarkers.Forfeit, StringComparison.OrdinalIgnoreCase) ||
            txDesc.Contains(LedgerMarkers.Penalty, StringComparison.OrdinalIgnoreCase))
            return "forfeit";

        // Refund (debit from platform = giving money back)
        if (wt.Type == WalletTransactionType.Debit)
            return "refund";

        // Default: platform commission
        if (desc.Contains(LedgerMarkers.Commission, StringComparison.OrdinalIgnoreCase) ||
            desc.Contains(LedgerMarkers.Platform, StringComparison.OrdinalIgnoreCase) ||
            wt.Type == WalletTransactionType.Credit)
            return "commission";

        return "commission";
    }

    private static string GetBucketKey(DateTime date, string granularity)
    {
        return granularity.ToLowerInvariant() switch
        {
            "week" => GetIsoWeekKey(date),
            "month" => date.ToString("yyyy-MM"),
            _ => date.ToString("yyyy-MM-dd"), // day
        };
    }

    private static string GetIsoWeekKey(DateTime date)
    {
        // Monday-based ISO week
        var dayOfWeek = ((int)date.DayOfWeek + 6) % 7; // Mon=0 … Sun=6
        var monday = date.AddDays(-dayOfWeek);
        return monday.ToString("yyyy-MM-dd");
    }
}
