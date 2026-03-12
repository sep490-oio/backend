using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.CancelInboundShipment;

public sealed record CancelInboundShipmentCommand(
    Guid   ShipmentId,
    string Reason
) : ICommand;

internal sealed class CancelInboundShipmentCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    IShippingService shippingService,
    ILogger<CancelInboundShipmentCommandHandler> logger)
    : ICommandHandler<CancelInboundShipmentCommand>
{
    public async Task<UnitResult<Error>> Handle(
        CancelInboundShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.ShipmentId);
        var now        = clock.UtcNow;

        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        var result = shipment.Cancel(request.Reason, now);
        if (result.IsFailure)
            return result.Error;

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

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "InboundShipment {ShipmentId} cancelled. Reason: {Reason}.",
            shipment.Id.Value, request.Reason);

        return UnitResult.Success<Error>();
    }
}