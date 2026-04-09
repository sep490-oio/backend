using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.UpdateInboundPackageExternalStatus;

public sealed record UpdateInboundPackageExternalStatusCommand(
    string ClientOrderCode,
    string Status
) : ICommand;

internal sealed class UpdateInboundPackageExternalStatusCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    ILogger<UpdateInboundPackageExternalStatusCommandHandler> logger)
    : ICommandHandler<UpdateInboundPackageExternalStatusCommand>
{
    public async Task<UnitResult<Error>> Handle(
        UpdateInboundPackageExternalStatusCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ClientOrderCode;
        var now  = clock.UtcNow;

        var siblings = await db.Set<InboundShipment>()
            .Where(s => s.ClientOrderCode == code && s.ShipmentMode == InboundShipmentMode.ExternalCarrier)
            .ToListAsync(cancellationToken);

        if (siblings.Count == 0)
            return WarehouseErrors.InboundShipment.NotFound(code);

        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);

        if (!isStaffRole && siblings.Any(s => s.SellerId != currentUser.UserId))
            return WarehouseErrors.InboundShipment.NotFound(code);

        var newStatus = InboundShipmentStatus.FromId(request.Status);
        if (newStatus.HasNoValue)
            return Error.Validation(
                "Inbound", "InboundShipment.InvalidStatus",
                $"Unknown shipment status: '{request.Status}'.");

        var advanced = 0;
        foreach (var sibling in siblings)
        {
            var result = sibling.ManuallyAdvanceStatus(newStatus.Value, now, isStaffRole);
            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Sibling InboundShipment {SiblingId} could not advance to '{Status}': {Error}",
                    sibling.Id.Value, request.Status, result.Error);
                continue;
            }
            advanced++;
        }

        if (advanced == 0)
            return WarehouseErrors.InboundShipment.InvalidTransition;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "InboundPackage {Code} advanced to '{Status}' ({Advanced}/{Total}).",
            code, request.Status, advanced, siblings.Count);

        return UnitResult.Success<Error>();
    }
}
