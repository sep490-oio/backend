using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminAuctionFinancials;

public sealed record AdminAuctionFinancialDto(
    Guid Id,
    string Source, // "Wallet" or "VNPay"
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    string Description,
    DateTimeOffset CreatedAt
);

public sealed record GetAdminAuctionFinancialsQuery(Guid AuctionId, string? Type = null) 
    : IQuery<IReadOnlyList<AdminAuctionFinancialDto>>;

internal sealed class GetAdminAuctionFinancialsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetAdminAuctionFinancialsQuery, IReadOnlyList<AdminAuctionFinancialDto>>
{
    public async Task<Result<IReadOnlyList<AdminAuctionFinancialDto>, Error>> Handle(
        GetAdminAuctionFinancialsQuery request,
        CancellationToken cancellationToken)
    {
        var targetAuctionId = AuctionId.From(request.AuctionId);
        var auctionIdStr = request.AuctionId.ToString();
        var financials = new List<AdminAuctionFinancialDto>();

        // Find associated order
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.AuctionId == targetAuctionId, cancellationToken);
        var orderIdStr = order?.Id.ToString();
        var orderNumberStr = order?.OrderNumber.Value;

        // 1. Fetch Wallet Transactions
        var walletTransactionsQuery = dbContext.Set<WalletTransaction>()
            .AsNoTracking()
            .Include(wt => wt.Transaction)
            .Where(wt => 
                (wt.Transaction != null && wt.Transaction.AuctionId == targetAuctionId)
                || (wt.Description != null && wt.Description.Contains(auctionIdStr))
            );

        var walletTransactions = await walletTransactionsQuery.ToListAsync(cancellationToken);

        // Include wallet transactions related to the order if any exist
        if (order != null)
        {
            var targetOrderId = order.Id;
            var orderWalletTxns = await dbContext.Set<WalletTransaction>()
                .AsNoTracking()
                .Include(wt => wt.Transaction)
                .Where(wt => 
                    (wt.Transaction != null && wt.Transaction.OrderId == targetOrderId)
                    || (wt.Description != null && wt.Description.Contains(orderIdStr!))
                    || (wt.Description != null && wt.Description.Contains(orderNumberStr!))
                )
                .ToListAsync(cancellationToken);
            
            // Merge uniquely
            walletTransactions = walletTransactions.UnionBy(orderWalletTxns, wt => wt.Id).ToList();
        }

        foreach (var wt in walletTransactions)
        {
            var mappedType = wt.Type.Id;
            // Map wallet semantic types to the expected filtering types (deposit, refund, fee, payment)
            if (wt.Type.Id == "hold") mappedType = "deposit";
            else if (wt.Type.Id == "release") mappedType = "refund";
            else if (wt.Type.Id == "credit" && wt.Description != null && (wt.Description.Contains("Compensation") || wt.Description.Contains("forfeit"))) mappedType = "fee";
            else if (wt.Type.Id == "debit" && wt.Description != null && wt.Description.Contains("Payment")) mappedType = "payment";

            financials.Add(new AdminAuctionFinancialDto(
                Id: wt.Id.Value,
                Source: "Wallet",
                Type: mappedType,
                Amount: wt.Amount,
                Currency: "VND",
                Status: "Completed", // Wallet transactions are instantaneous
                Description: wt.Description ?? "Wallet Transaction",
                CreatedAt: wt.CreatedAt
            ));
        }

        // 2. Fetch VNPay Transactions
        var gatewayTxnsQuery = dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.AuctionId == targetAuctionId);

        var gatewayTxns = await gatewayTxnsQuery.ToListAsync(cancellationToken);

        if (order != null)
        {
            var targetOrderId = order.Id;
            var orderGatewayTxns = await dbContext.Set<Transaction>()
                .AsNoTracking()
                .Where(t => t.OrderId == targetOrderId)
                .ToListAsync(cancellationToken);
            
            gatewayTxns = gatewayTxns.UnionBy(orderGatewayTxns, t => t.Id).ToList();
        }

        var gatewayTxnIdsInWallet = walletTransactions
            .Where(wt => wt.Transaction != null)
            .Select(wt => wt.Transaction!.Id)
            .ToHashSet();

        foreach (var t in gatewayTxns)
        {
            if (gatewayTxnIdsInWallet.Contains(t.Id))
                continue;

            financials.Add(new AdminAuctionFinancialDto(
                Id: t.Id.Value,
                Source: "VNPay",
                Type: t.Type.Id,
                Amount: t.Amount.Amount,
                Currency: t.Amount.Currency.Id,
                Status: t.Status.Id,
                Description: $"VNPay {t.Type.Id} (Txn: {t.TransactionNumber.Value})",
                CreatedAt: t.CreatedAt
            ));
        }

        // Filter by type
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var filterType = request.Type.ToLowerInvariant();
            financials = financials.Where(f => f.Type.ToLowerInvariant() == filterType).ToList();
        }

        return financials.OrderByDescending(f => f.CreatedAt).ToList();
    }
}
