using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.UpdateExternalShipmentStatus;

public sealed record UpdateExternalShipmentStatusCommand(
    Guid   ShipmentId,
    string Status      // awaiting_pickup | in_transit | arrived | cancelled | failed
) : ICommand<InboundShipmentDto>;

internal sealed class UpdateExternalShipmentStatusCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<UpdateExternalShipmentStatusCommandHandler> logger)
    : ICommandHandler<UpdateExternalShipmentStatusCommand, InboundShipmentDto>
{
    public async Task<Result<InboundShipmentDto, Error>> Handle(
        UpdateExternalShipmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        var newStatus = InboundShipmentStatus.FromId(request.Status);
        if (newStatus.HasNoValue)
            return Error.Validation(
                "Inbound","InboundShipment.InvalidStatus",
                $"Unknown shipment status: '{request.Status}'.");

        var result = shipment.ManuallyAdvanceStatus(newStatus.Value, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "External InboundShipment {ShipmentId} manually advanced to '{Status}' by staff.",
            shipment.Id.Value, request.Status);

        return shipment.ToDto();
    }
}