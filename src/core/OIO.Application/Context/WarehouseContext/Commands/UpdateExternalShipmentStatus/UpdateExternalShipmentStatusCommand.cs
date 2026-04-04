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
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
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
    ICurrentUser currentUser,
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

        // Ownership check — seller can only advance their own shipments;
        // WarehouseStaff/Inspector/Admin can advance any external shipment
        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);
        if (!isStaffRole && shipment.SellerId != currentUser.UserId)
            return WarehouseErrors.InboundShipment.NotFound(request.ShipmentId.ToString());

        var newStatus = InboundShipmentStatus.FromId(request.Status);
        if (newStatus.HasNoValue)
            return Error.Validation(
                "Inbound","InboundShipment.InvalidStatus",
                $"Unknown shipment status: '{request.Status}'.");

        var result = shipment.ManuallyAdvanceStatus(newStatus.Value, clock.UtcNow, isStaffRole);
        if (result.IsFailure) return result.Error;

        // Advance all sibling shipments with same ClientOrderCode (batch shipments share one package)
        var siblings = await db.Set<InboundShipment>()
            .Where(s => s.ClientOrderCode == shipment.ClientOrderCode
                     && s.Id != shipment.Id
                     && s.ShipmentMode == InboundShipmentMode.ExternalCarrier)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings)
        {
            var siblingResult = sibling.ManuallyAdvanceStatus(newStatus.Value, clock.UtcNow, isStaffRole);
            if (siblingResult.IsFailure)
            {
                logger.LogWarning(
                    "Sibling InboundShipment {SiblingId} could not advance to '{Status}': {Error}",
                    sibling.Id.Value, request.Status, siblingResult.Error);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var advancedCount = siblings.Count(s => s.Status == newStatus.Value) + 1;
        logger.LogInformation(
            "External InboundShipment {ShipmentId} (and {SiblingCount} siblings) advanced to '{Status}'.",
            shipment.Id.Value, advancedCount - 1, request.Status);

        return shipment.ToDto();
    }
}