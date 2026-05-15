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
    string? Type = null,
    string? Category = null) : IQuery<PlatformWalletTransactionsResultDto>;

public sealed record PlatformWalletTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Description,
    string? Category,
    DateTime CreatedAt,
    Guid? SourceOrderId = null,
    string? SourceOrderNumber = null,
    string? SourceItemTitle = null,
    Guid? SourceAuctionId = null);

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

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var cat = request.Category.ToLowerInvariant();
            if (cat == "commission")
            {
                query = query.Where(wt => wt.Description != null && (wt.Description.ToLower().Contains("commission") || wt.Description.ToLower().Contains("platform")) || wt.Type.Id == "credit");
            }
            else if (cat == "inspection_fee")
            {
                query = query.Where(wt => wt.Description != null && wt.Description.ToLower().Contains("inspection"));
            }
            else if (cat == "forfeit")
            {
                query = query.Where(wt => wt.Description != null && (wt.Description.ToLower().Contains("forfeit") || wt.Description.ToLower().Contains("penalty")));
            }
            else if (cat == "refund")
            {
                query = query.Where(wt => wt.Type.Id == "debit");
            }
            // other
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(wt => wt.Transaction)
            .OrderByDescending(wt => wt.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Resolve order numbers for wallet transactions that have a linked Transaction with OrderId
        var orderIds = items
            .Where(wt => wt.Transaction?.OrderId is not null)
            .Select(wt => wt.Transaction!.OrderId!)
            .Distinct()
            .ToList();

        var orderLookup = orderIds.Count > 0
            ? await dbContext.Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { o.Id, OrderNumber = o.OrderNumber.Value, o.AuctionId })
                .ToDictionaryAsync(o => o.Id, cancellationToken)
            : new();

        // Resolve item titles from auctions linked to orders
        var auctionIds = orderLookup.Values
            .Select(o => o.AuctionId)
            .Distinct()
            .ToList();

        var auctionTitleLookup = auctionIds.Count > 0
            ? await dbContext.Set<OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
                .AsNoTracking()
                .Where(a => auctionIds.Contains(a.Id))
                .Select(a => new { a.Id, Title = a.Item.Title.Value })
                .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken)
            : new();

        // 2) Resolve items/auctions for inspection rejection fees (no OrderId, but transaction number has inspection ID)
        var rejectionInspectionIds = items
            .Where(wt => wt.Transaction?.OrderId is null && wt.Transaction?.TransactionNumber.Value.StartsWith("FEE-INSP-REJ-") == true)
            .Select(wt => 
            {
                var parts = wt.Transaction!.TransactionNumber.Value.Split('-');
                if (parts.Length == 4 && Guid.TryParse(parts[3], out var id)) return id;
                return Guid.Empty;
            })
            .Where(id => id != Guid.Empty)
            .ToList();

        var rejectionAuctionLookup = new Dictionary<Guid, (string ItemTitle, Guid? AuctionId)>();
        if (rejectionInspectionIds.Count > 0)
        {
            var inspectionTypedIds = rejectionInspectionIds
                .Select(id => OIO.Domain.Context.WarehouseContext.ValueObjects.Ids.WarehouseInspectionId.From(id))
                .ToList();

            var inspectionItems = await dbContext.Set<OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.WarehouseInspection>()
                .AsNoTracking()
                .Where(wi => inspectionTypedIds.Contains(wi.Id))
                .Select(wi => new { InspectionId = wi.Id.Value, wi.ItemId })
                .ToListAsync(cancellationToken);

            var itemIds = inspectionItems.Select(x => OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId.From(x.ItemId)).ToList();
            
            var itemInfos = await dbContext.Set<OIO.Domain.Context.CatalogContext.Aggregates.Items.Item>()
                .AsNoTracking()
                .Where(i => itemIds.Contains(i.Id))
                .Select(i => new { ItemId = i.Id, Title = i.Title.Value })
                .ToDictionaryAsync(i => i.ItemId.Value, cancellationToken);

            var latestAuctionsList = await dbContext.Set<OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
                .AsNoTracking()
                .Where(a => itemIds.Contains(a.ItemId))
                .Select(a => new { ItemId = a.ItemId, AuctionId = a.Id, a.CreatedAt })
                .ToListAsync(cancellationToken);

            var latestAuctions = latestAuctionsList
                .GroupBy(a => a.ItemId)
                .Select(g => g.OrderByDescending(a => a.CreatedAt).First())
                .ToDictionary(a => a.ItemId.Value);

            foreach (var insp in inspectionItems)
            {
                if (itemInfos.TryGetValue(insp.ItemId, out var itemInfo))
                {
                    Guid? auctionId = latestAuctions.TryGetValue(insp.ItemId, out var auction) ? auction.AuctionId.Value : null;
                    rejectionAuctionLookup[insp.InspectionId] = (itemInfo.Title, auctionId);
                }
            }
        }

        var dtos = items.Select(wt =>
        {
            var orderId = wt.Transaction?.OrderId;
            var orderInfo = orderId.HasValue && orderLookup.TryGetValue(orderId.Value, out var info) ? info : null;
            var itemTitle = orderInfo is not null && auctionTitleLookup.TryGetValue(orderInfo.AuctionId, out var title) ? title : null;
            Guid? sourceAuctionId = orderInfo?.AuctionId.Value;

            if (orderId is null && wt.Transaction?.TransactionNumber.Value.StartsWith("FEE-INSP-REJ-") == true)
            {
                var parts = wt.Transaction.TransactionNumber.Value.Split('-');
                if (parts.Length == 4 && Guid.TryParse(parts[3], out var inspId) && rejectionAuctionLookup.TryGetValue(inspId, out var rejectionInfo))
                {
                    itemTitle = rejectionInfo.ItemTitle;
                    sourceAuctionId = rejectionInfo.AuctionId;
                }
            }

            return new PlatformWalletTransactionDto(
                Id: wt.Id.Value,
                Type: wt.Type.Id,
                Amount: wt.Amount,
                BalanceBefore: wt.BalanceBefore,
                BalanceAfter: wt.BalanceAfter,
                Description: wt.Description,
                Category: ClassifyTransaction(wt),
                CreatedAt: wt.CreatedAt,
                SourceOrderId: orderId?.Value,
                SourceOrderNumber: orderInfo?.OrderNumber,
                SourceItemTitle: itemTitle,
                SourceAuctionId: sourceAuctionId
            );
        }).ToList();

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
