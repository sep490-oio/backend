using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using static OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions.GetCompletedAuctionsQueryHandler;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminTransactions;

public record AdminTransactionFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public string? Type { get; init; }
    public Guid? UserId { get; init; }
    public Guid? OrderId { get; init; }
}

public sealed record GetAdminTransactionsQuery(
    AdminTransactionFilterParameters Parameters) : IQuery<PagedList<PaymentTransactionDto>>;

internal sealed class GetAdminTransactionsQueryHandler
    : IQueryHandler<GetAdminTransactionsQuery, PagedList<PaymentTransactionDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminTransactionsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<PaymentTransactionDto>, Error>> Handle(
        GetAdminTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Transaction>()
            .AsNoTracking()
            .AsQueryable();

        if (parameters.UserId.HasValue)
            query = query.Where(x => x.UserId == UserId.From(parameters.UserId.Value));

        if (parameters.OrderId.HasValue)
            query = query.Where(x => x.OrderId == OrderId.From(parameters.OrderId.Value));

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = TransactionStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Transaction.InvalidStatus", "Unsupported transaction status.");

            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Type))
        {
            var type = TransactionType.FromId(parameters.Type);
            if (type.HasNoValue)
                return Error.Validation("type", "Transaction.InvalidType", "Unsupported transaction type.");

            query = query.Where(x => x.Type == type.Value);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var count = await query.CountAsync(cancellationToken);

        // Project basic DTO first (no navigation JOINs — keeps the SQL simple).
        var items = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(count, parameters, cancellationToken);

        // ── Batch-enrich with display names ──────────────────────────────
        var userIds = items.Items
            .Select(t => UserId.From(t.UserId))
            .Distinct()
            .ToList();

        var users = userIds.Count > 0
            ? await _dbContext.Set<User>()
                .AsNoTracking()
                .Include(u => u.SellerProfile)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken)
            : [];
        var usersById = users.ToDictionary(u => u.Id.Value);

        // Resolve order numbers for order-linked transactions.
        var orderIds = items.Items
            .Where(t => t.OrderId.HasValue)
            .Select(t => OrderId.From(t.OrderId!.Value))
            .Distinct()
            .ToList();

        var orderNumbers = orderIds.Count > 0
            ? await _dbContext.Set<Order>()
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { Id = o.Id.Value, Number = o.OrderNumber.Value })
                .ToListAsync(cancellationToken)
            : [];
        var orderNumberById = orderNumbers.ToDictionary(o => o.Id, o => o.Number);

        // Resolve auction item titles for auction-linked transactions.
        var auctionIds = items.Items
            .Where(t => t.AuctionId.HasValue)
            .Select(t => AuctionId.From(t.AuctionId!.Value))
            .Distinct()
            .ToList();

        var auctionTitles = auctionIds.Count > 0
            ? await _dbContext.Set<Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
                .AsNoTracking()
                .Where(a => auctionIds.Contains(a.Id))
                .Select(a => new { Id = a.Id.Value, Title = a.Item.Title.Value })
                .ToListAsync(cancellationToken)
            : [];
        var titleByAuctionId = auctionTitles.ToDictionary(a => a.Id, a => a.Title);

        var enriched = items.Items
            .Select(t => t with
            {
                UserDisplayName = usersById.TryGetValue(t.UserId, out var u)
                    ? ResolveUserDisplayName(u) : null,
                OrderNumber = t.OrderId.HasValue && orderNumberById.TryGetValue(t.OrderId.Value, out var on)
                    ? on : null,
                AuctionItemTitle = t.AuctionId.HasValue && titleByAuctionId.TryGetValue(t.AuctionId.Value, out var at)
                    ? at : null,
            })
            .ToList();

        return new PagedList<PaymentTransactionDto>(
            enriched, items.Metadata.TotalCount,
            items.Metadata.CurrentPage, items.Metadata.PageSize);
    }
}
