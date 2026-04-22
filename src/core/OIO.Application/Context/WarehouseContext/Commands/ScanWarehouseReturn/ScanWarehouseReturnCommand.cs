using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.ScanWarehouseReturn;

/// <summary>
/// Seller scans the warehouse-staff-issued return QR on parcel arrival.
/// Validates the signed token, asserts
/// <see cref="WarehouseToSellerShipmentStatus.InTransit"/>, and flips the
/// shipment to <see cref="WarehouseToSellerShipmentStatus.Delivered"/>
/// WITHOUT an evidence guard (parallel to OrderReturn.MarkSellerReceived —
/// receipt evidence is enforced at ConfirmBySeller via the aggregate guard).
/// </summary>
public sealed record ScanWarehouseReturnCommand(
    Guid ShipmentId,
    string QrToken) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        ScanWarehouseReturnCommand.Check()
            .WithOwnerName("ScanWarehouseReturn")
            .Field(ShipmentId).NotEmptyGuid()
            .Field(QrToken).NotWhiteSpace();
}

internal sealed class ScanWarehouseReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IReturnShipmentQrTokenService qrTokenService,
    ILogger<ScanWarehouseReturnCommandHandler> logger)
    : ICommandHandler<ScanWarehouseReturnCommand>
{
    private const string WarehouseToSellerKind = "warehouse_to_seller";

    public async Task<UnitResult<Error>> Handle(
        ScanWarehouseReturnCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        if (shipment.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "WarehouseToSellerShipment.ScanForbidden",
                "Only the seller can scan a warehouse-return shipment.");

        // Validate signed QR token.
        var payloadResult = qrTokenService.Validate(request.QrToken);
        if (payloadResult.IsFailure)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                $"QR token is invalid: {payloadResult.Error.Message}");

        var payload = payloadResult.Value;

        if (payload.Kind != WarehouseToSellerKind)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                $"QR token kind '{payload.Kind}' is not valid for warehouse returns.");

        if (payload.ShipmentOrReturnId != shipment.Id.Value)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                "QR token does not match this shipment.");

        var now = clock.UtcNow;
        if (payload.ExpiresAt <= now)
            return Error.Validation(
                "qrToken",
                "Scan.ExpiredToken",
                "QR token has expired.");

        // Scan status guard: can only be scanned when in InTransit (after shipped,
        // before delivered). Parallel to OrderReturn's ReturnInTransit guard.
        if (shipment.Status != WarehouseToSellerShipmentStatus.InTransit)
            return Error.Conflict(
                "WarehouseToSellerShipment.InvalidStatus",
                $"Can only scan shipments that are in transit, but shipment is '{shipment.Status.Id}'.");

        var deliverResult = shipment.MarkDelivered(deliveredAt: now, nowUtc: now);
        if (deliverResult.IsFailure)
            return deliverResult.Error;

        dbContext.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ScanWarehouseReturn: WarehouseToSellerShipment {ShipmentId} flipped to Delivered by seller {SellerId}.",
            shipment.Id.Value, currentUser.UserId.Value);

        return UnitResult.Success<Error>();
    }
}
