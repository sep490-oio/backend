using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetPlatformWalletTransactions;

// ── Query ───────────────────────────────────────────────────────────────

public sealed record GetPlatformWalletTransactionsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Type = null) : IQuery<PlatformWalletTransactionsResultDto>;

public sealed record PlatformWalletTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Description,
    string? Category,
    DateTime CreatedAt);

public sealed record PlatformWalletTransactionsResultDto(
    IReadOnlyList<PlatformWalletTransactionDto> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    string Currency);

// ── Handler ─────────────────────────────────────────────────────────────

internal sealed class GetPlatformWalletTransactionsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPlatformWalletTransactionsQuery, PlatformWalletTransactionsResultDto>
{
    public async Task<Result<PlatformWalletTransactionsResultDto, Error>> Handle(
        GetPlatformWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var platformWallet = await dbContext.Set<Wallet>()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Type == WalletType.Platform, cancellationToken);

        if (platformWallet is null)
            return Error.NotFound("PlatformWallet.NotFound", "Platform wallet not found.");

        var currency = platformWallet.WalletFunds.Currency.Id;

        var query = dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Where(wt => wt.WalletId == platformWallet.Id);

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = request.Type.ToLowerInvariant();
            query = query.Where(wt => wt.Type.Id == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(wt => wt.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(wt => new PlatformWalletTransactionDto(
            Id: wt.Id.Value,
            Type: wt.Type.Id,
            Amount: wt.Amount,
            BalanceBefore: wt.BalanceBefore,
            BalanceAfter: wt.BalanceAfter,
            Description: wt.Description,
            Category: ClassifyTransaction(wt),
            CreatedAt: wt.CreatedAt
        )).ToList();

        return new PlatformWalletTransactionsResultDto(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: request.PageNumber,
            PageSize: request.PageSize,
            Currency: currency);
    }

    private static string ClassifyTransaction(WalletTransaction wt)
    {
        var desc = wt.Description ?? string.Empty;

        if (desc.Contains("inspection", StringComparison.OrdinalIgnoreCase))
            return "inspection_fee";

        if (desc.Contains("forfeit", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("penalty", StringComparison.OrdinalIgnoreCase))
            return "forfeit";

        if (wt.Type == WalletTransactionType.Debit)
            return "refund";

        if (desc.Contains("commission", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Platform", StringComparison.OrdinalIgnoreCase) ||
            wt.Type == WalletTransactionType.Credit)
            return "commission";

        return "other";
    }
}
