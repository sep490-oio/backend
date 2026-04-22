using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.RecordWarehouseReturnDeliveryFailure;

/// <summary>
/// Warehouse-staff records that a warehouse→seller shipment failed to deliver.
/// Transitions shipment to <c>ReturnedToWarehouse</c> and warehouse item to
/// <c>AwaitingDisposition</c> so admins can route it further.
/// </summary>
public sealed record RecordWarehouseReturnDeliveryFailureCommand(
    Guid ShipmentId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RecordWarehouseReturnDeliveryFailureCommand.Check()
            .WithOwnerName("RecordWarehouseReturnDeliveryFailure")
            .Field(ShipmentId)
            .NotEmptyGuid()
            .Field(Reason)
            .NotWhiteSpace();
    }
}

internal sealed class RecordWarehouseReturnDeliveryFailureCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<RecordWarehouseReturnDeliveryFailureCommandHandler> logger)
    : ICommandHandler<RecordWarehouseReturnDeliveryFailureCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RecordWarehouseReturnDeliveryFailureCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == shipment.WarehouseItemId, cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(shipment.WarehouseItemId.Value.ToString());

        var now = clock.UtcNow;

        var failureResult = shipment.RecordDeliveryFailure(request.Reason.Trim(), now);
        if (failureResult.IsFailure)
            return failureResult.Error;

        var dispositionResult = warehouseItem.MarkAwaitingDisposition(now);
        if (dispositionResult.IsFailure)
            return dispositionResult.Error;

        dbContext.Update(shipment);
        dbContext.Update(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "WarehouseToSellerShipment {ShipmentId} delivery failed (reason: {Reason}). " +
            "WarehouseItem {WarehouseItemId} moved to AwaitingDisposition.",
            shipment.Id.Value, request.Reason, warehouseItem.Id.Value);

        return UnitResult.Success<Error>();
    }
}
