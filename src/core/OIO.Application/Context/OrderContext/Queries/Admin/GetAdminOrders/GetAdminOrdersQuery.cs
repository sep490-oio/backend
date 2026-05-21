using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.Admin.GetAdminOrders;

public record GetAdminOrdersQueryFilter : PagedParameters
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}

public sealed record GetAdminOrdersQuery(GetAdminOrdersQueryFilter Parameters)
    : IQuery<PagedList<AdminOrderListItemDto>>;

internal sealed class GetAdminOrdersQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetAdminOrdersQuery, PagedList<AdminOrderListItemDto>>
{
    public async Task<Result<PagedList<AdminOrderListItemDto>, Error>> Handle(
        GetAdminOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        var pageNumber = p.PageNumber ?? 1;
        var pageSize = p.PageSize ?? 20;

        var query = dbContext.Set<Order>()
            .AsNoTracking()
            .AsQueryable();

        // Status filter
        if (!string.IsNullOrWhiteSpace(p.Status))
        {
            query = query.Where(o => o.Status.Id == p.Status);
        }

        // Search: order number or order id
        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var needle = p.Search.Trim();
            var needleLike = $"%{needle}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.OrderNumber.Value, needleLike) ||
                EF.Functions.ILike(o.Id.Value.ToString(), needleLike));
        }

        // Date range
        if (p.FromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= p.FromDate.Value);
        if (p.ToDate.HasValue)
            query = query.Where(o => o.CreatedAt <= p.ToDate.Value);

        // Count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch paged orders
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (orders.Count == 0)
        {
            return new PagedList<AdminOrderListItemDto>(
                Array.Empty<AdminOrderListItemDto>(), totalCount, pageNumber, pageSize);
        }

        // Load user display names
        var userIds = orders
            .SelectMany(o => new[] { o.BuyerId, o.SellerId })
            .Distinct()
            .ToList();
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var usersById = users.ToDictionary(u => u.Id.Value);

        // Load auction items for titles + images
        var auctionIds = orders.Select(o => o.AuctionId).Distinct().ToList();
        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Where(a => auctionIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        var auctionById = auctions.ToDictionary(a => a.Id);

        var items = orders.Select(o =>
        {
            var buyer = usersById.TryGetValue(o.BuyerId.Value, out var b) ? b : null;
            var seller = usersById.TryGetValue(o.SellerId.Value, out var s) ? s : null;
            var auction = auctionById.TryGetValue(o.AuctionId, out var a) ? a : null;

            var primaryImageUrl = auction?.Item?.Media
                .Where(m => m.IsPrimary)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Info.SecureUrl)
                .FirstOrDefault();

            return new AdminOrderListItemDto(
                Id: o.Id.Value,
                OrderNumber: o.OrderNumber.Value,
                AuctionId: o.AuctionId.Value,
                Status: o.Status.Id,
                TotalAmount: o.Pricing.TotalAmount.Amount,
                Currency: o.Currency,
                BuyerDisplayName: GetCompletedAuctionsQueryHandler.ResolveUserDisplayName(buyer),
                SellerDisplayName: GetCompletedAuctionsQueryHandler.ResolveSellerDisplayName(seller),
                ItemTitle: auction?.Item?.Title.Value,
                ItemPrimaryImageUrl: primaryImageUrl,
                CreatedAt: o.CreatedAt,
                PaidAt: o.PaidAt,
                CompletedAt: o.CompletedAt,
                CancelledAt: o.CancelledAt);
        }).ToList();

        return new PagedList<AdminOrderListItemDto>(items, totalCount, pageNumber, pageSize);
    }
}
