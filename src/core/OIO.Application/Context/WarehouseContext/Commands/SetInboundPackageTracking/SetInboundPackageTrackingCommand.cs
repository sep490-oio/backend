using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.SetInboundPackageTracking;

public sealed record SetInboundPackageTrackingCommand(
    string ClientOrderCode,
    string TrackingNumber
) : ICommand;

internal sealed class SetInboundPackageTrackingCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser)
    : ICommandHandler<SetInboundPackageTrackingCommand>
{
    public async Task<UnitResult<Error>> Handle(
        SetInboundPackageTrackingCommand request,
        CancellationToken cancellationToken)
    {
        var code = request.ClientOrderCode;
        var now  = clock.UtcNow;

        var siblings = await db.Set<InboundShipment>()
            .Where(s => s.ClientOrderCode == code)
            .ToListAsync(cancellationToken);

        if (siblings.Count == 0)
            return WarehouseErrors.InboundShipment.NotFound(code);

        var isStaffRole = currentUser.IsInRole(App.Roles.Catalogs.WarehouseStaff)
                       || currentUser.IsInRole(App.Roles.Catalogs.Inspector)
                       || currentUser.IsInRole(App.Roles.Catalogs.Admin);

        if (!isStaffRole && siblings.Any(s => s.SellerId != currentUser.UserId))
            return WarehouseErrors.InboundShipment.NotFound(code);

        if (siblings.Any(s => s.ShipmentMode != InboundShipmentMode.ExternalCarrier))
            return WarehouseErrors.InboundShipment.NotExternalCarrier;

        foreach (var sibling in siblings)
        {
            var result = sibling.SetExternalTrackingNumber(request.TrackingNumber, now);
            if (result.IsFailure) return result.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
