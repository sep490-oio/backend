using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.CancelOutboundShipment;

public sealed record CancelOutboundShipmentCommand(
    Guid   ShipmentId,
    string Reason
) : ICommand;

internal sealed class CancelOutboundShipmentCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    IShippingService shippingService,
    ILogger<CancelOutboundShipmentCommandHandler> logger)
    : ICommandHandler<CancelOutboundShipmentCommand>
{
    public async Task<UnitResult<Error>> Handle(
        CancelOutboundShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(request.ShipmentId);
        var now        = clock.UtcNow;

        var shipment = await db.Set<OutboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.OutboundShipment.NotFound(request.ShipmentId.ToString());

        var result = shipment.Cancel(request.Reason, now);
        if (result.IsFailure)
            return result.Error;

        // Cancel with GHN if already booked
        // Cancel with GHN if already booked
        if (shipment.CarrierTrackingNumber is not null)
        {
            var config = await db.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(c => c.ProviderCode == shipment.ProviderCode && c.IsActive, cancellationToken);

            if (config is not null)
            {
                var cancelResult = await shippingService.CancelShipmentAsync(
                    shipment.ProviderCode.Id,
                    shipment.CarrierTrackingNumber,
                    config,
                    cancellationToken);

                if (cancelResult.IsFailure)
                {
                    logger.LogWarning(
                        "GHN cancel failed for {TrackingNumber}: {Error}",
                        shipment.CarrierTrackingNumber, cancelResult.Error.Message);
                    return cancelResult.Error;
                }
            }
        }
        // Release warehouse item back to Stored
        var warehouseItem = await db.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == shipment.WarehouseItemId, cancellationToken);

        if (warehouseItem is not null && warehouseItem.StorageLocationId is { } locationId)
            _ = warehouseItem.Store(locationId, string.Empty, now);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "OutboundShipment {ShipmentId} cancelled. Reason: {Reason}.",
            shipment.Id.Value, request.Reason);
        
        return UnitResult.Success<Error>();
    }
}