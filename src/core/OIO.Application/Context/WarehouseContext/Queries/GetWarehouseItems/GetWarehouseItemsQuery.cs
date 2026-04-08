using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItems;

public sealed record GetWarehouseItemsQuery(GetWarehouseItemsQueryFilter Parameters)
    : IQuery<PagedList<WarehouseItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetWarehouseItemsQuery.Check()
            .WithOwnerName("GetWarehouseItems")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(WarehouseItemStatus.All.Select(s => s.Id)));
    }
}

internal sealed class GetWarehouseItemsQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseItemsQuery, PagedList<WarehouseItemDto>>
{
    public async Task<Result<PagedList<WarehouseItemDto>, Error>> Handle(
        GetWarehouseItemsQuery request,
        CancellationToken cancellationToken)
    {
        var warehouseItems    = db.Set<WarehouseItem>().AsNoTracking();
        var inboundShipments  = db.Set<InboundShipment>().AsNoTracking();
        var storageLocations  = db.Set<WarehouseStorageLocation>().AsNoTracking();
        var items             = db.Set<Item>().AsNoTracking();
        var users             = db.Set<User>().AsNoTracking();

        var query = from w in warehouseItems
                    join inb in inboundShipments on w.InboundShipmentId equals inb.Id
                    join item in items on w.ItemId equals item.Id.Value
                    join seller in users on inb.SellerId equals seller.Id
                    join loc in storageLocations on w.StorageLocationId equals loc.Id into locG
                    from loc in locG.DefaultIfEmpty()
                    select new { w, inb, item, seller, loc };

        // ── Filtering ─────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Parameters.Status))
        {
            var status = WarehouseItemStatus.FromId(request.Parameters.Status.Trim().ToLowerInvariant());
            if (status.HasValue)
                query = query.Where(x => x.w.Status == status.Value);
        }

        if (request.Parameters.StorageLocationId.HasValue)
        {
            var locId = WarehouseStorageLocationId.From(request.Parameters.StorageLocationId.Value);
            query = query.Where(x => x.w.StorageLocationId == locId);
        }

        if (request.Parameters.ItemId.HasValue)
            query = query.Where(x => x.w.ItemId == request.Parameters.ItemId.Value);

        if (request.Parameters.InboundShipmentId.HasValue)
        {
            var shipmentId = InboundShipmentId.From(request.Parameters.InboundShipmentId.Value);
            query = query.Where(x => x.w.InboundShipmentId == shipmentId);
        }

        if (request.Parameters.SellerId.HasValue)
        {
            var sId = UserId.From(request.Parameters.SellerId.Value);
            query = query.Where(x => x.inb.SellerId == sId);
        }

        // ── Search Logic ───────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(request.Parameters.SearchTerm))
        {
            var search = request.Parameters.SearchTerm.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.item.Title.Value.ToLower().Contains(search) ||
                x.inb.ClientOrderCode.ToLower().Contains(search) ||
                (x.loc != null && x.loc.Label.ToLower().Contains(search)) ||
                x.seller.UserName.Value.ToLower().Contains(search) ||
                (x.seller.Profile.Name.DisplayName != null && x.seller.Profile.Name.DisplayName.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var alerts = await query
            .OrderByDescending(x => x.w.CreatedAt)
            .Select(x => new WarehouseItemDto(
                Id:                   x.w.Id.Value,
                ItemId:               x.w.ItemId,
                InboundShipmentId:    x.w.InboundShipmentId.Value,
                InboundShipmentCode:  x.inb.ClientOrderCode,
                StorageLocationId:    x.w.StorageLocationId != null ? (Guid?)x.w.StorageLocationId.Value : null,
                StorageLocationLabel: x.loc != null ? x.loc.Label : null,
                ItemTitle:            x.item.Title.Value,
                SellerId:             x.inb.SellerId.Value,
                SellerName:           x.seller.Profile.Name.DisplayName ?? x.seller.UserName.Value,
                ItemImageUrl:         x.item.Media.FirstOrDefault(m => m.IsPrimary) != null 
                    ? x.item.Media.FirstOrDefault(m => m.IsPrimary)!.Info.SecureUrl 
                    : x.item.Media.FirstOrDefault() != null ? x.item.Media.FirstOrDefault()!.Info.SecureUrl : null,
                Status:               x.w.Status.Id,
                ReceivedAt:           x.w.ReceivedAt,
                CreatedAt:            x.w.CreatedAt,
                ModifiedAt:           x.w.ModifiedAt,
                Media:                x.w.Media.Select(m => new WarehouseItemMediaDto(
                    Id:           m.Id.Value,
                    ResourceType: m.ResourceType,
                    IsPrimary:    m.IsPrimary,
                    SortOrder:    m.SortOrder,
                    SecureUrl:    m.Info.SecureUrl,
                    FileName:     m.Info.FileName
                )).OrderBy(m => m.SortOrder).ToList()
            ))
            .ToPagedListAsync(totalCount, request.Parameters, cancellationToken);

        return Result.Success<PagedList<WarehouseItemDto>, Error>(alerts);
    }
}
