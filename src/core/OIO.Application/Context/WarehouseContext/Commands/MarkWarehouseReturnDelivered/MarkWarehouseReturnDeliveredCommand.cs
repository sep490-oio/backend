using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.MarkWarehouseReturnDelivered;

/// <summary>
/// Warehouse-staff marks a warehouse→seller return shipment as delivered.
/// Used for manual carriers where no external tracking updates the status
/// automatically (e.g. staff hand-delivers the package to the seller).
/// Transitions: InTransit → Delivered.
/// </summary>
public sealed record MarkWarehouseReturnDeliveredCommand(
    Guid ShipmentId) : ICommand;

internal sealed class MarkWarehouseReturnDeliveredCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<MarkWarehouseReturnDeliveredCommandHandler> logger)
    : ICommandHandler<MarkWarehouseReturnDeliveredCommand>
{
    public async Task<UnitResult<Error>> Handle(
        MarkWarehouseReturnDeliveredCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        var now = clock.UtcNow;

        var result = shipment.MarkDelivered(deliveredAt: now, nowUtc: now);
        if (result.IsFailure)
            return result.Error;

        dbContext.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseToSellerShipment {ShipmentId} marked as delivered by staff.",
            shipment.Id.Value);

        return UnitResult.Success<Error>();
    }
}
