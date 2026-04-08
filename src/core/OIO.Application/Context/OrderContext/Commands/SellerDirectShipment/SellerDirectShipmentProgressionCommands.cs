using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Commands.SellerOrderProgression;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.SeedWork.Errors;
using ShipmentAggregate = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;

namespace OIO.Application.Context.OrderContext.Commands.SellerDirectShipment;

public sealed record SetSellerDirectShipmentCarrierInfoCommand(
    Guid ShipmentId,
    string ExternalCarrierName,
    string ExternalTrackingCode) : ICommand<SellerDirectShipmentDto>;

public sealed record MarkSellerDirectShipmentPickedUpCommand(Guid ShipmentId)
    : ICommand<SellerDirectShipmentDto>;

public sealed record MarkSellerDirectShipmentOnDeliveringCommand(Guid ShipmentId)
    : ICommand<SellerDirectShipmentDto>;

public sealed record MarkSellerDirectShipmentDeliveredCommand(Guid ShipmentId)
    : ICommand<SellerDirectShipmentDto>;

public sealed record BuyerAcknowledgeDirectShipmentReceivedCommand(Guid ShipmentId)
    : ICommand<SellerDirectShipmentDto>;

/// <summary>
/// Shared loader for shipment + linked order. Authorizes the caller against
/// either seller or buyer ownership and returns 404 on mismatch.
/// </summary>
internal static class SellerDirectShipmentLoader
{
    public static async Task<Result<(ShipmentAggregate Shipment, Order Order), Error>> LoadAsync(
        IDbContext dbContext,
        Guid shipmentIdValue,
        Guid currentUserGuid,
        bool requireSeller,
        CancellationToken cancellationToken)
    {
        var shipmentId = SellerDirectShipmentId.From(shipmentIdValue);
        var shipment = await dbContext.Set<ShipmentAggregate>()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("SellerDirectShipment.NotFound", "Direct shipment was not found.");

        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(shipment.OrderId);

        // Authz mismatches → 404 (no enumeration).
        var ownerId = requireSeller ? order.SellerId.Value : order.BuyerId.Value;
        if (ownerId != currentUserGuid)
            return Error.NotFound("SellerDirectShipment.NotFound", "Direct shipment was not found.");

        return (shipment, order);
    }
}

internal sealed class SetSellerDirectShipmentCarrierInfoCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<SetSellerDirectShipmentCarrierInfoCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        SetSellerDirectShipmentCarrierInfoCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, _) = loaded.Value;
        var result = shipment.SetCarrierInfo(request.ExternalCarrierName, request.ExternalTrackingCode, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.ToDto();
    }
}

internal sealed class MarkSellerDirectShipmentPickedUpCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ISender sender)
    : ICommandHandler<MarkSellerDirectShipmentPickedUpCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        MarkSellerDirectShipmentPickedUpCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, order) = loaded.Value;
        var result = shipment.MarkPickedUp(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Cross-dispatch: advance the parent Order in lockstep so seller-side
        // order screens reflect the same status without an extra UI hop.
        var orderResult = await sender.Send(new MarkOrderPickedUpCommand(order.Id.Value), cancellationToken);
        if (orderResult.IsFailure) return orderResult.Error;

        return shipment.ToDto();
    }
}

internal sealed class MarkSellerDirectShipmentOnDeliveringCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ISender sender)
    : ICommandHandler<MarkSellerDirectShipmentOnDeliveringCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        MarkSellerDirectShipmentOnDeliveringCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, order) = loaded.Value;
        var result = shipment.MarkOnDelivering(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var orderResult = await sender.Send(new MarkOrderOnDeliveringCommand(order.Id.Value), cancellationToken);
        if (orderResult.IsFailure) return orderResult.Error;

        return shipment.ToDto();
    }
}

internal sealed class MarkSellerDirectShipmentDeliveredCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ISender sender)
    : ICommandHandler<MarkSellerDirectShipmentDeliveredCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        MarkSellerDirectShipmentDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, order) = loaded.Value;
        var result = shipment.MarkDelivered(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var orderResult = await sender.Send(new MarkOrderDeliveredCommand(order.Id.Value), cancellationToken);
        if (orderResult.IsFailure) return orderResult.Error;

        return shipment.ToDto();
    }
}

public sealed record SetSellerDirectShipmentDispatchDetailsCommand(
    Guid ShipmentId,
    string CarrierName,
    string TrackingNumber,
    DateTime ShippedAt,
    List<Guid> PackagePhotoMediaUploadIds) : ICommand<SellerDirectShipmentDto>;

internal sealed class SetSellerDirectShipmentDispatchDetailsCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<SetSellerDirectShipmentDispatchDetailsCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        SetSellerDirectShipmentDispatchDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, _) = loaded.Value;

        var now = clock.UtcNow;

        var carrierResult = shipment.SetCarrierInfo(request.CarrierName, request.TrackingNumber, now);
        if (carrierResult.IsFailure) return carrierResult.Error;

        shipment.RecordSellerDeclaredShippedAt(request.ShippedAt, now);

        var newPhotoIds = (request.PackagePhotoMediaUploadIds ?? new List<Guid>())
            .Distinct()
            .ToList();

        if (newPhotoIds.Count > 0)
        {
            var ids = newPhotoIds.Select(MediaUploadId.From).ToList();
            var uploads = await dbContext.Set<MediaUpload>()
                .Where(m => ids.Contains(m.Id))
                .ToListAsync(cancellationToken);
            var byId = uploads.ToDictionary(u => u.Id.Value);

            foreach (var id in newPhotoIds)
            {
                if (!byId.TryGetValue(id, out var upload))
                    return Error.NotFound("Media.NotFound", $"Media upload {id} not found.");
                shipment.AddEvidence(
                    SellerDirectShipmentEvidenceKind.SellerPackagePhoto,
                    upload.Id,
                    upload.Info.SecureUrl ?? string.Empty,
                    currentUser.UserId.Value,
                    now);
            }
        }

        // After applying any new photos, the shipment must have at least one
        // package photo total. We don't require new uploads on every edit when
        // photos already exist on the aggregate.
        var totalPackagePhotos = shipment.Evidence.Count(e =>
            e.Kind == SellerDirectShipmentEvidenceKind.SellerPackagePhoto);
        if (totalPackagePhotos == 0)
            return Error.Validation(
                "packagePhotoMediaUploadIds",
                "SellerDirectShipment.DispatchDetailsIncomplete",
                "At least one package photo is required.");

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.ToDto();
    }
}

public sealed record AddSellerDirectShipmentHandoverProofsCommand(
    Guid ShipmentId,
    List<Guid> HandoverProofMediaUploadIds) : ICommand<SellerDirectShipmentDto>;

internal sealed class AddSellerDirectShipmentHandoverProofsCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddSellerDirectShipmentHandoverProofsCommand, SellerDirectShipmentDto>
{
    private static readonly HashSet<SellerDirectShipmentStatus> AllowedStatuses = new()
    {
        SellerDirectShipmentStatus.CarrierBooked,
        SellerDirectShipmentStatus.PickedUp,
        SellerDirectShipmentStatus.OnDelivering,
    };

    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        AddSellerDirectShipmentHandoverProofsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HandoverProofMediaUploadIds is null || request.HandoverProofMediaUploadIds.Count == 0)
            return Error.Validation(
                "handoverProofMediaUploadIds",
                "SellerDirectShipment.HandoverProofs.Required",
                "At least one handover proof is required.");

        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: true, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, _) = loaded.Value;

        if (!AllowedStatuses.Contains(shipment.Status))
            return Error.Validation(
                "status",
                "SellerDirectShipment.HandoverProofs.InvalidStatus",
                "Handover proofs can only be added while the shipment is in transit.");

        var now = clock.UtcNow;

        var ids = request.HandoverProofMediaUploadIds
            .Distinct()
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await dbContext.Set<MediaUpload>()
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);
        var byId = uploads.ToDictionary(u => u.Id.Value);

        foreach (var id in request.HandoverProofMediaUploadIds.Distinct())
        {
            if (!byId.TryGetValue(id, out var upload))
                return Error.NotFound("Media.NotFound", $"Media upload {id} not found.");
            shipment.AddEvidence(
                SellerDirectShipmentEvidenceKind.SellerHandoverProof,
                upload.Id,
                upload.Info.SecureUrl ?? string.Empty,
                currentUser.UserId.Value,
                now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.ToDto();
    }
}

public sealed record SubmitProofOfDeliveryCommand(
    Guid ShipmentId,
    List<Guid> DeliveryPhotoMediaUploadIds,
    string PackageCondition,
    string? ConditionNotes,
    string Source) : ICommand<SellerDirectShipmentDto>;

internal sealed class SubmitProofOfDeliveryCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOrderDeliveryService orderDeliveryService,
    IClock clock)
    : ICommandHandler<SubmitProofOfDeliveryCommand, SellerDirectShipmentDto>
{
    private static readonly HashSet<string> AllowedConditions = new(StringComparer.OrdinalIgnoreCase)
    {
        "sealed_intact",
        "outer_damage",
        "wet_or_torn",
        "tamper_suspected",
        "wrong_parcel",
        "other"
    };

    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        SubmitProofOfDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DeliveryPhotoMediaUploadIds is null || request.DeliveryPhotoMediaUploadIds.Count == 0)
            return Error.Validation(
                "deliveryPhotoMediaUploadIds",
                "SellerDirectShipment.ProofOfDelivery.PhotoRequired",
                "At least one delivery photo is required.");

        if (!AllowedConditions.Contains(request.PackageCondition))
            return Error.Validation(
                "packageCondition",
                "SellerDirectShipment.ProofOfDelivery.InvalidCondition",
                "Package condition is not one of the allowed values.");

        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: false, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, order) = loaded.Value;
        var now = clock.UtcNow;

        // Transition the shipment to Delivered BEFORE stamping proof so that
        // a disallowed source state (Draft/CarrierBooked or any terminal
        // state) short-circuits with a proper error rather than silently
        // stamping receipt fields on an incomplete shipment.
        var deliveredResult = shipment.MarkDeliveredFromBuyerProof(now);
        if (deliveredResult.IsFailure) return deliveredResult.Error;

        // Advance the parent Order + start the return-decision window via the
        // shared service so the buyer action gate (needs order.Delivered +
        // open decision window) unlocks on the order detail page.
        var orderDeliveredResult = await orderDeliveryService.MarkAsDeliveredAsync(order, now, cancellationToken);
        if (orderDeliveredResult.IsFailure) return orderDeliveredResult.Error;

        var ackResult = shipment.AcknowledgeReceived(now);
        if (ackResult.IsFailure) return ackResult.Error;

        shipment.SetBuyerCondition(request.PackageCondition, request.ConditionNotes, now);

        var ids = request.DeliveryPhotoMediaUploadIds
            .Distinct()
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await dbContext.Set<MediaUpload>()
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var byId = uploads.ToDictionary(u => u.Id.Value);
        foreach (var id in request.DeliveryPhotoMediaUploadIds.Distinct())
        {
            if (!byId.TryGetValue(id, out var upload))
                return Error.NotFound("Media.NotFound", $"Media upload {id} not found.");
            shipment.AddEvidence(
                SellerDirectShipmentEvidenceKind.BuyerDeliveryPhoto,
                upload.Id,
                upload.Info.SecureUrl ?? string.Empty,
                currentUser.UserId.Value,
                now);
        }

        if (!string.Equals(request.PackageCondition, "sealed_intact", StringComparison.OrdinalIgnoreCase))
        {
            shipment.FlagManualReview("buyer_reported_damage", now);

            var alert = MonitoringAlert.Create(
                entityType: "SellerDirectShipment",
                entityId: shipment.Id.Value,
                alertType: "buyer_reported_damage",
                severity: AlertSeverity.High,
                payload: System.Text.Json.JsonSerializer.Serialize(new
                {
                    shipmentId = shipment.Id.Value,
                    orderId = order.Id.Value,
                    buyerId = order.BuyerId.Value,
                    sellerId = order.SellerId.Value,
                    packageCondition = request.PackageCondition,
                    conditionNotes = request.ConditionNotes,
                    source = request.Source
                }),
                nowUtc: now);
            dbContext.Set<MonitoringAlert>().Add(alert);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.ToDto();
    }
}

public sealed record ValidateSellerDirectShipmentScanResultDto(Guid ShipmentId, Guid OrderId);

public sealed record ValidateSellerDirectShipmentScanCommand(string Token)
    : ICommand<ValidateSellerDirectShipmentScanResultDto>;

internal sealed class ValidateSellerDirectShipmentScanCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IClock clock,
    ISellerDirectShipmentTokenService tokenService)
    : ICommandHandler<ValidateSellerDirectShipmentScanCommand, ValidateSellerDirectShipmentScanResultDto>
{
    private static readonly HashSet<string> TerminalStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "accepted",
        "disputed",
        "completed"
    };

    // Only in-transit and already-delivered shipments accept buyer scans.
    // Draft/CarrierBooked means the package has not left the seller yet, so
    // a buyer scanning the QR would be nonsensical.
    private static readonly HashSet<string> AllowedScanStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "picked_up",
        "on_delivering",
        "delivered"
    };

    public async Task<Result<ValidateSellerDirectShipmentScanResultDto, Error>> Handle(
        ValidateSellerDirectShipmentScanCommand request,
        CancellationToken cancellationToken)
    {
        var parsed = tokenService.Validate(request.Token);
        if (parsed.IsFailure) return parsed.Error;

        var payload = parsed.Value;
        var shipmentId = SellerDirectShipmentId.From(payload.ShipmentId);
        var shipment = await dbContext.Set<ShipmentAggregate>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("SellerDirectShipment.NotFound", "Direct shipment was not found.");

        if (payload.BuyerId != currentUser.UserId.Value)
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.NotOwned",
                "Token is not valid for the current user.");

        if (TerminalStatuses.Contains(shipment.Status.Id))
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.TerminalStatus",
                "Shipment is in a terminal state; token can no longer be used.");

        if (!AllowedScanStatuses.Contains(shipment.Status.Id))
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.NotInTransit",
                "Shipment has not been dispatched yet; token cannot be used.");

        if (payload.Version != shipment.QrTokenVersion)
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.Stale",
                "Token version does not match the current QR token.");

        if (shipment.QrTokenRevokedAt is not null)
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.Revoked",
                "Token has been revoked.");

        var issuedAt = DateTime.SpecifyKind(payload.IssuedAt, DateTimeKind.Utc);
        if (clock.UtcNow - issuedAt > TimeSpan.FromDays(90))
            return Error.Validation(
                "token",
                "SellerDirectShipment.Token.Expired",
                "Token has expired.");

        return new ValidateSellerDirectShipmentScanResultDto(shipment.Id.Value, shipment.OrderId.Value);
    }
}

internal sealed class BuyerAcknowledgeDirectShipmentReceivedCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<BuyerAcknowledgeDirectShipmentReceivedCommand, SellerDirectShipmentDto>
{
    public async Task<Result<SellerDirectShipmentDto, Error>> Handle(
        BuyerAcknowledgeDirectShipmentReceivedCommand request,
        CancellationToken cancellationToken)
    {
        var loaded = await SellerDirectShipmentLoader.LoadAsync(
            dbContext, request.ShipmentId, currentUser.UserId.Value,
            requireSeller: false, cancellationToken);
        if (loaded.IsFailure) return loaded.Error;

        var (shipment, _) = loaded.Value;
        var result = shipment.AcknowledgeReceived(clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return shipment.ToDto();
    }
}
