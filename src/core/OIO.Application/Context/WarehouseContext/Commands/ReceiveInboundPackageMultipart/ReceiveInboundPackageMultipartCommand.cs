using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundPackageByCode;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Application.Context.WarehouseContext.Commands.ReceiveInboundPackageMultipart;

public sealed record ReceiveInboundPackageMultipartCommand(
    string      ClientOrderCode,
    string?     Notes,
    IFormFile?  FrontPhoto,
    IFormFile?  ShippingLabelPhoto,
    IFormFile?  SealConditionPhoto,
    IFormFile?  InsideContentsPhoto
) : ICommand<InboundPackageDetailDto>;

internal sealed class ReceiveInboundPackageMultipartCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IMediaDirectUploadService directUpload,
    ISender sender,
    ILogger<ReceiveInboundPackageMultipartCommandHandler> logger)
    : ICommandHandler<ReceiveInboundPackageMultipartCommand, InboundPackageDetailDto>
{
    private const string ReceivingFolder = "warehouse/receiving";

    public async Task<Result<InboundPackageDetailDto, e>> Handle(
        ReceiveInboundPackageMultipartCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FrontPhoto is null || request.FrontPhoto.Length == 0)
            return e.Validation("frontPhoto", "Warehouse.FrontPhotoRequired", "A front photo is required to receive the package.");

        if (string.IsNullOrWhiteSpace(request.ClientOrderCode))
            return e.Validation("clientOrderCode", "Warehouse.ClientOrderCodeRequired", "Client order code is required.");

        var code = request.ClientOrderCode;

        var siblings = await db.Set<InboundShipment>()
            .Where(s => s.ClientOrderCode == code)
            .ToListAsync(cancellationToken);

        if (siblings.Count == 0)
            return WarehouseErrors.InboundShipment.NotFound(code);

        if (siblings.All(s => s.Status == InboundShipmentStatus.Arrived ||
                              s.Status == InboundShipmentStatus.Inspected ||
                              s.Status == InboundShipmentStatus.Completed))
        {
            var existingItems = await db.Set<WarehouseItem>()
                .AnyAsync(w => siblings.Select(s => s.Id).Contains(w.InboundShipmentId), cancellationToken);
            if (existingItems)
                return e.Conflict("Warehouse.PackageAlreadyReceived", "This package has already been received.");
        }

        var now = clock.UtcNow;
        var staffId = currentUser.UserId;

        // ── Upload photos ─────────────────────────────────────────────────────
        var slots = new[]
        {
            (File: request.FrontPhoto,          Key: "frontPhoto"),
            (File: request.ShippingLabelPhoto,  Key: "shippingLabelPhoto"),
            (File: request.SealConditionPhoto,  Key: "sealConditionPhoto"),
            (File: request.InsideContentsPhoto, Key: "insideContentsPhoto"),
        };

        var uploadedUrls = new List<string>();

        foreach (var slot in slots.Where(s => s.File is not null && s.File.Length > 0))
        {
            await using var stream = slot.File!.OpenReadStream();
            var uploadResult = await directUpload.UploadAsync(
                fileStream: stream,
                fileName:   slot.File.FileName,
                folder:     ReceivingFolder,
                ct:         cancellationToken);

            if (uploadResult.IsFailure)
                return uploadResult.Error;

            uploadedUrls.Add(uploadResult.Value.SecureUrl);
        }

        // ── Build receipt JSON and write to every sibling's ExtraData ─────────
        var receiptPayload = new
        {
            packageReceipt = new
            {
                photos = uploadedUrls,
                notes = request.Notes,
                receivedAt = now,
                receivedBy = staffId.Value
            }
        };
        var receiptJson = JsonSerializer.Serialize(receiptPayload);
        var newExtraData = ShipmentExtraData.From(receiptJson);

        foreach (var shipment in siblings)
        {
            shipment.SetExtraData(newExtraData, now);

            if (shipment.Status != InboundShipmentStatus.Arrived &&
                shipment.Status != InboundShipmentStatus.Inspected &&
                shipment.Status != InboundShipmentStatus.Completed)
            {
                var arriveResult = shipment.RecordArrived(now);
                if (arriveResult.IsFailure)
                    return arriveResult.Error;
            }
        }

        // ── Create WarehouseItems (if missing) and MarkReceived ───────────────
        var existingWarehouseItems = await db.Set<WarehouseItem>()
            .Where(w => siblings.Select(s => s.Id).Contains(w.InboundShipmentId))
            .ToListAsync(cancellationToken);

        var existingByShipment = existingWarehouseItems.ToDictionary(w => w.InboundShipmentId);

        foreach (var shipment in siblings)
        {
            if (!existingByShipment.TryGetValue(shipment.Id, out var wi))
            {
                wi = WarehouseItem.Create(
                    itemId: shipment.ItemId,
                    inboundShipmentId: shipment.Id,
                    now: now);
                db.Set<WarehouseItem>().Add(wi);
            }

            if (wi.Status == WarehouseItemStatus.Pending)
                wi.MarkReceived(now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Package {ClientOrderCode} received — {Count} items, {Photos} photos.",
            code, siblings.Count, uploadedUrls.Count);

        var detailResult = await sender.Send(new GetInboundPackageByCodeQuery(code), cancellationToken);
        if (detailResult.IsFailure)
            return detailResult.Error;
        return detailResult.Value;
    }
}
