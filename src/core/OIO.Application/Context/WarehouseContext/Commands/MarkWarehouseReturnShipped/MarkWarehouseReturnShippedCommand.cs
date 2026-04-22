using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.MarkWarehouseReturnShipped;

/// <summary>
/// Warehouse-staff records the return carrier + tracking number for a
/// pending <see cref="WarehouseToSellerShipment"/>. Transitions Pending → InTransit.
/// </summary>
public sealed record MarkWarehouseReturnShippedCommand(
    Guid ShipmentId,
    string ProviderCode,
    string TrackingNumber,
    DateTime ShippedAt) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return MarkWarehouseReturnShippedCommand.Check()
            .WithOwnerName("MarkWarehouseReturnShipped")
            .Field(ShipmentId)
            .NotEmptyGuid()
            .Field(ProviderCode)
            .NotWhiteSpace()
            .Field(TrackingNumber)
            .NotWhiteSpace();
    }
}

internal sealed class MarkWarehouseReturnShippedCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IReturnShipmentQrTokenService qrTokenService,
    ILogger<MarkWarehouseReturnShippedCommandHandler> logger)
    : ICommandHandler<MarkWarehouseReturnShippedCommand>
{
    public async Task<UnitResult<Error>> Handle(
        MarkWarehouseReturnShippedCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        var now = clock.UtcNow;
        var shipResult = shipment.MarkShipped(
            providerCode:   request.ProviderCode.Trim(),
            trackingNumber: request.TrackingNumber.Trim(),
            shippedAt:      request.ShippedAt,
            nowUtc:         now);

        if (shipResult.IsFailure)
            return shipResult.Error;

        // C7: issue a signed return-scoped QR token so the seller can scan on arrival.
        var qrToken = qrTokenService.Issue(
            kind:              "warehouse_to_seller",
            shipmentOrReturnId: shipment.Id.Value,
            issuedAt:          now,
            expiresAt:         now.AddDays(30));

        var qrResult = shipment.IssueReturnQr(qrToken, now);
        if (qrResult.IsFailure)
            return qrResult.Error;

        dbContext.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseToSellerShipment {ShipmentId} marked shipped via {ProviderCode}/{TrackingNumber}.",
            shipment.Id.Value, request.ProviderCode, request.TrackingNumber);

        return UnitResult.Success<Error>();
    }
}
