using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.SetExternalTrackingNumber;

public sealed record SetExternalTrackingNumberCommand(
    Guid   ShipmentId,
    string TrackingNumber
) : ICommand<InboundShipmentDto>;

internal sealed class SetExternalTrackingNumberCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<SetExternalTrackingNumberCommand, InboundShipmentDto>
{
    public async Task<Result<InboundShipmentDto, Error>> Handle(
        SetExternalTrackingNumberCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = InboundShipmentId.From(request.ShipmentId);

        var shipment = await db.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        var result = shipment.SetExternalTrackingNumber(request.TrackingNumber, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}