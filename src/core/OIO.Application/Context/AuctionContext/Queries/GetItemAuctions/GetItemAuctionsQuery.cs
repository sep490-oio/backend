using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemAuctions;

public sealed record GetItemAuctionsQuery(Guid ItemId) : IQuery<List<AuctionListItemDto>>;

internal sealed class GetItemAuctionsQueryHandler
    : IQueryHandler<GetItemAuctionsQuery, List<AuctionListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;

    public GetItemAuctionsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
    }

    public async Task<Result<List<AuctionListItemDto>, Error>> Handle(
        GetItemAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var itemId = ItemId.From(request.ItemId);

        // Verify the item belongs to the current user or the user is an admin.
        var isAdmin = _currentUser.IsAuthenticated && _currentUser.IsInRole(App.Roles.Catalogs.Admin);

        var auctions = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Include(a => a.BuyNowReservations)
            .AsSplitQuery()
            .Where(a => a.Item.Id == itemId && (a.Item.SellerId == _currentUser.UserId || isAdmin))
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var auctionIds = auctions.Select(a => a.Id).ToList();
        var ordersByAuctionId = await _dbContext.Set<Order>()
            .AsNoTracking()
            .Where(o => auctionIds.Contains(o.AuctionId))
            .ToDictionaryAsync(o => o.AuctionId, o => o.Id.Value, cancellationToken);

        var extensionThreshold = _runtimeSettings.Auction.ExtensionThreshold;

        var dtos = auctions
            .Select(a => a.ToListItemDto(nowUtc, extensionThreshold, false, ordersByAuctionId.GetValueOrDefault(a.Id)))
            .ToList();

        return dtos;
    }
}
