using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Queries.GetItemById;

internal sealed class GetItemByIdQueryHandler
    : IQueryHandler<GetItemByIdQuery, ItemDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetItemByIdQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<ItemDto, Error>> Handle(
        GetItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(x => x.Media),
            cancellationToken:cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var isOwner = _currentUser.IsAuthenticated && item.SellerId == _currentUser.UserId;
        var isAdmin = _currentUser.IsAuthenticated && _currentUser.IsInRole(App.Roles.Catalogs.Admin);
        var isPublicItem =
            item.Status == ItemStatus.Approved ||
            item.Status == ItemStatus.Active ||
            item.Status == ItemStatus.InAuction ||
            item.Status == ItemStatus.Sold;

        if (!isOwner && !isAdmin && !isPublicItem)
            return AuctionErrors.Item.NotFound(itemId);

        var hasInbound = await _dbContext.Set<InboundShipment>()
            .AnyAsync(s => s.ItemId == itemId.Value &&
                           s.Status != InboundShipmentStatus.Cancelled &&
                           s.Status != InboundShipmentStatus.Failed,
                cancellationToken);

        return item.ToDto(hasInbound);
    }
}
