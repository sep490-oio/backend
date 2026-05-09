using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.SubmitOutboundShipmentReceiptProof;

public sealed record SubmitOutboundShipmentReceiptProofCommand(
    Guid ShipmentId,
    List<Guid> ReceiptPhotoMediaUploadIds) : ICommand<BuyerOutboundShipmentDetailDto>;

internal sealed class SubmitOutboundShipmentReceiptProofCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IMediaRelocationService mediaRelocationService,
    IOrderDeliveryService orderDeliveryService)
    : ICommandHandler<SubmitOutboundShipmentReceiptProofCommand, BuyerOutboundShipmentDetailDto>
{
    public async Task<Result<BuyerOutboundShipmentDetailDto, Error>> Handle(
        SubmitOutboundShipmentReceiptProofCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ReceiptPhotoMediaUploadIds.Count == 0)
            return Error.Validation(
                "receiptPhotoMediaUploadIds",
                "ReceiptProof.AtLeastOnePhoto",
                "At least one receipt photo is required.");

        var shipmentId = OutboundShipmentId.From(request.ShipmentId);
        var shipment = await db.Set<OutboundShipment>()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        var order = await db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null || order.BuyerId != currentUser.UserId)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        // Load + validate media uploads
        var mediaUploadIds = request.ReceiptPhotoMediaUploadIds
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await db.Set<MediaUpload>()
            .Where(x => mediaUploadIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != mediaUploadIds.Count)
        {
            var missingIds = mediaUploadIds
                .Where(id => uploads.All(u => u.Id != id))
                .Select(id => id.Value.ToString());
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        if (uploads.Any(x => x.UserId != currentUser.UserId))
            return MediaErrors.NotOwnedByUser(
                string.Join(", ", uploads.Where(x => x.UserId != currentUser.UserId).Select(x => x.Id.Value)));

        if (uploads.Any(x => !x.IsConfirmed))
            return MediaErrors.NotConfirm;

        if (uploads.Any(x => x.IsLinked))
            return MediaErrors.AlreadyLinked;

        var now = clock.UtcNow;

        foreach (var upload in uploads)
        {
            shipment.AddEvidence(now, "buyer_receipt_photo", upload);

            var linkResult = upload.LinkToEntity(shipment.Id, now);
            if (linkResult.IsFailure) return linkResult.Error;

            await mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }

        // Auto-acknowledge receipt when proof is submitted
        var ackResult = shipment.AcknowledgeReceivedByBuyer(now, "receipt_proof");
        if (ackResult.IsFailure)
            return ackResult.Error;

        // ── Auto-deliver: when the buyer submits receipt proof but the
        // shipment/order haven't been marked as delivered by warehouse staff
        // yet, promote both to Delivered so the buyer can immediately proceed
        // to inspect-and-accept without waiting for a staff action.
        if (shipment.Status != OutboundShipmentStatus.Delivered)
        {
            var deliverResult = shipment.RecordDelivered(deliveredAt: now, now: now);
            // Idempotent — ignore failure if already delivered
            if (deliverResult.IsFailure &&
                shipment.Status != OutboundShipmentStatus.Delivered)
                return deliverResult.Error;
        }

        var orderDeliveredResult = await orderDeliveryService.MarkAsDeliveredAsync(order, now, cancellationToken);
        if (orderDeliveredResult.IsFailure) return orderDeliveredResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuyerOutboundShipmentDetailBuilder.BuildAsync(db, shipment, order, cancellationToken);
    }
}
