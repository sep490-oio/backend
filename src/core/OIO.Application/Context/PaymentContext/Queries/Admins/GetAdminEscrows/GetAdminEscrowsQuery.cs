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
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using static OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions.GetCompletedAuctionsQueryHandler;

namespace OIO.Application.Context.PaymentContext.Queries.Admins.GetAdminEscrows;

public record AdminEscrowFilterParameters : PagedParameters
{
    public string? Status { get; init; }
    public Guid? OrderId { get; init; }
    public Guid? BuyerId { get; init; }
    public Guid? SellerId { get; init; }
    public string? SearchTerm { get; init; }
    public DateTimeOffset? FromDate { get; init; }
    public DateTimeOffset? ToDate { get; init; }
}

public sealed record GetAdminEscrowsQuery(
    AdminEscrowFilterParameters Parameters) : IQuery<PagedList<EscrowDto>>;

internal sealed class GetAdminEscrowsQueryHandler
    : IQueryHandler<GetAdminEscrowsQuery, PagedList<EscrowDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminEscrowsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<EscrowDto>, Error>> Handle(
        GetAdminEscrowsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Escrow>()
            .AsNoTracking()
            .Include(x => x.Order)
            .Include(x => x.ReleaseEvents)
            .AsQueryable();

        if (parameters.OrderId.HasValue)
            query = query.Where(x => x.OrderId == OrderId.From(parameters.OrderId.Value));

        if (parameters.BuyerId.HasValue)
            query = query.Where(x => x.Order.BuyerId == UserId.From(parameters.BuyerId.Value));

        if (parameters.SellerId.HasValue)
            query = query.Where(x => x.Order.SellerId == UserId.From(parameters.SellerId.Value));

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = EscrowStatus.FromId(parameters.Status);
            if (status.HasNoValue)
                return Error.Validation("status", "Escrow.InvalidStatus", "Unsupported escrow status.");

            query = query.Where(x => x.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var term = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Order.OrderNumber.Value.ToLower().Contains(term));
        }

        if (parameters.FromDate.HasValue)
            query = query.Where(x => x.HeldAt >= parameters.FromDate.Value);

        if (parameters.ToDate.HasValue)
            query = query.Where(x => x.HeldAt <= parameters.ToDate.Value);

        query = query.OrderByDescending(x => x.HeldAt);

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Select(x => x.ToDto(null, null, null, null))
            .ToPagedListAsync(count, parameters, cancellationToken);

        // ── Batch-enrich with display names ──────────────────────────────
        var userIds = items.Items
            .SelectMany(e => new[] { UserId.From(e.BuyerId), UserId.From(e.SellerId) })
            .Concat(items.Items.SelectMany(e => e.ReleaseEvents ?? []).Where(r => r.CreatedBy != null).Select(r => UserId.From(r.CreatedBy!.Value)))
            .Distinct()
            .ToList();

        var users = userIds.Count > 0
            ? await _dbContext.Set<User>()
                .AsNoTracking()
                .Include(u => u.Profile)
                .Include(u => u.SellerProfile)
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken)
            : [];
        var usersById = users.ToDictionary(u => u.Id.Value);

        // Resolve order numbers.
        var orderIds = items.Items
            .Select(e => OrderId.From(e.OrderId))
            .Distinct()
            .ToList();

        var orderData = orderIds.Count > 0
            ? await _dbContext.Set<Order>()
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .Select(o => new { Id = o.Id.Value, Number = o.OrderNumber.Value, AuctionId = o.AuctionId.Value })
                .ToListAsync(cancellationToken)
            : [];
        var orderNumberById = orderData.ToDictionary(o => o.Id, o => o.Number);

        // Resolve auction item titles via order → auction.
        var auctionIds = orderData
            .Select(o => AuctionId.From(o.AuctionId))
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

        // Build auction-title lookup keyed by orderId.
        var titleByOrderId = orderData.ToDictionary(
            o => o.Id,
            o => titleByAuctionId.TryGetValue(o.AuctionId, out var t) ? t : null);

        var enriched = items.Items
            .Select(e => e with
            {
                OrderNumber = orderNumberById.TryGetValue(e.OrderId, out var on) ? on : null,
                BuyerDisplayName = usersById.TryGetValue(e.BuyerId, out var buyer)
                    ? ResolveUserDisplayName(buyer) : null,
                SellerDisplayName = usersById.TryGetValue(e.SellerId, out var seller)
                    ? ResolveSellerDisplayName(seller) : null,
                AuctionItemTitle = titleByOrderId.TryGetValue(e.OrderId, out var at) ? at : null,
                ReleaseEvents = e.ReleaseEvents?.Select(r => r with
                {
                    CreatedByDisplayName = r.CreatedBy != null && usersById.TryGetValue(r.CreatedBy.Value, out var admin)
                        ? (admin.Profile?.Name?.DisplayName ?? admin.Profile?.Name?.FullName ?? admin.UserName.Value)
                        : null
                }).ToList()
            })
            .ToList();

        return new PagedList<EscrowDto>(
            enriched, items.Metadata.TotalCount,
            items.Metadata.CurrentPage, items.Metadata.PageSize);
    }
}
