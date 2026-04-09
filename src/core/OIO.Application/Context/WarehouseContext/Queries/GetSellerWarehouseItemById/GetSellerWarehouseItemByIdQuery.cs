using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetSellerWarehouseItemById;

public sealed record GetSellerWarehouseItemByIdQuery(Guid WarehouseItemId)
    : IQuery<SellerWarehouseItemDetailDto>;

internal sealed class GetSellerWarehouseItemByIdQueryHandler(
    IDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerWarehouseItemByIdQuery, SellerWarehouseItemDetailDto>
{
    public async Task<Result<SellerWarehouseItemDetailDto, Error>> Handle(
        GetSellerWarehouseItemByIdQuery request,
        CancellationToken cancellationToken)
    {
        var whId = WarehouseItemId.From(request.WarehouseItemId);

        var w = await db.Set<WarehouseItem>().AsNoTracking()
            .Include(x => x.Media)
            .FirstOrDefaultAsync(x => x.Id == whId, cancellationToken);

        if (w is null)
            return Result.Failure<SellerWarehouseItemDetailDto, Error>(
                Error.NotFound("SellerWarehouseItem.NotFound",
                    $"Warehouse item {request.WarehouseItemId} not found"));

        var shipment = await db.Set<InboundShipment>().AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == w.InboundShipmentId, cancellationToken);

        // Ownership gate: seller must own the shipment the item came from.
        if (shipment is null || shipment.SellerId != currentUser.UserId)
            return Result.Failure<SellerWarehouseItemDetailDto, Error>(
                Error.NotFound("SellerWarehouseItem.NotFound",
                    $"Warehouse item {request.WarehouseItemId} not found"));

        var itemId = ItemId.From(w.ItemId);
        var item = await db.Set<Item>().AsNoTracking()
            .Include(i => i.Media)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        WarehouseStorageLocation? location = null;
        if (w.StorageLocationId is { } locId)
        {
            location = await db.Set<WarehouseStorageLocation>().AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == locId, cancellationToken);
        }

        var inspection = await db.Set<WarehouseInspection>().AsNoTracking()
            .Where(x => x.WarehouseItemId == w.Id)
            .OrderByDescending(x => x.InspectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var outbound = await db.Set<OutboundShipment>().AsNoTracking()
            .Where(o => o.WarehouseItemId == w.Id)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var primaryItemMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                               ?? item?.Media.FirstOrDefault();

        var receiptMedia = w.Media
            .OrderBy(m => m.SortOrder)
            .Select(m => new SellerWarehouseItemReceiptMediaDto(
                Id:        m.Id.Value,
                Url:       m.Info.SecureUrl,
                CreatedAt: m.CreatedAt))
            .ToList();

        User? inspector = null;
        User? reviewer = null;
        if (inspection is not null)
        {
            inspector = await db.Set<User>().AsNoTracking()
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Id == inspection.InspectedBy, cancellationToken);

            if (inspection.ReviewedBy is { } reviewedById)
            {
                reviewer = await db.Set<User>().AsNoTracking()
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == reviewedById, cancellationToken);
            }
        }

        SellerWarehouseInspectionDetailDto? inspectionDto = null;
        if (inspection is not null)
        {
            var evidence = inspection.Evidence.ToSnapshots()
                .Where(s => !string.IsNullOrEmpty(s.SecureUrl))
                .Select(s => new WarehouseInspectionEvidenceDto(
                    s.PublicId,
                    s.Folder,
                    s.SecureUrl,
                    s.FileName,
                    s.Bytes,
                    s.Format,
                    s.Width,
                    s.Height,
                    s.DurationSeconds))
                .ToList();

            inspectionDto = new SellerWarehouseInspectionDetailDto(
                DecisionStatus:       inspection.DecisionStatus.Id,
                DeclaredCondition:    inspection.DeclaredCondition.Id,
                ConditionOnArrival:   inspection.ConditionOnArrival.Id,
                InspectionNotes:      inspection.InspectionNotes,
                DecisionReason:       inspection.DecisionReason,
                InspectedAt:          inspection.InspectedAt,
                ReviewedAt:           inspection.ReviewedAt,
                SellerConfirmedAt:    inspection.SellerConfirmedAt,
                InspectorDisplayName: inspector?.Profile?.Name.DisplayName ?? inspector?.UserName.Value,
                ReviewerDisplayName:  reviewer?.Profile?.Name.DisplayName ?? reviewer?.UserName.Value,
                Evidence:             evidence);
        }

        var flowStatus = SellerWarehouseFlowStatusResolver.Resolve(w, inspection, outbound);

        // Confirm-inspected-condition is allowed when the item itself is in
        // PendingConditionConfirmation AND the latest inspection is waiting on
        // seller confirmation. Mirrors ConfirmInspectedConditionCommandHandler.
        var canConfirmInspectedCondition =
            item is not null &&
            item.Status == ItemStatus.PendingConditionConfirmation &&
            inspection is not null &&
            inspection.DecisionStatus == WarehouseInspectionDecisionStatus.ConditionConfirmationRequired;

        var dto = new SellerWarehouseItemDetailDto(
            WarehouseItemId:               w.Id.Value,
            ItemId:                        w.ItemId,
            ItemTitle:                     item?.Title.Value,
            ItemImageUrl:                  primaryItemMedia?.Info.SecureUrl,
            InboundPackageCode:            shipment.ClientOrderCode,
            InboundShipmentId:             w.InboundShipmentId.Value,
            StorageLocationLabel:          location?.Label,
            ReceivedAt:                    w.ReceivedAt,
            UpdatedAt:                     w.ModifiedAt ?? w.CreatedAt,
            WarehouseFlowStatus:           flowStatus,
            WarehouseItemStatusRaw:        w.Status.Id,
            Inspection:                    inspectionDto,
            OutboundShipmentId:            outbound?.Id.Value,
            OutboundStatus:                outbound?.Status.Id,
            OutboundCarrierTrackingNumber: outbound?.CarrierTrackingNumber,
            OutboundShippingLabelUrl:      outbound?.ShippingLabelUrl,
            OutboundDispatchedAt:          outbound?.DispatchedAt,
            OutboundDeliveredAt:           outbound?.DeliveredAt,
            ReceiptMedia:                  receiptMedia,
            CanConfirmInspectedCondition:  canConfirmInspectedCondition);

        return Result.Success<SellerWarehouseItemDetailDto, Error>(dto);
    }
}
