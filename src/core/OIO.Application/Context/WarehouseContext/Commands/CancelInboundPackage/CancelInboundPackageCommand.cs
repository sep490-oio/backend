using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundPackages;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.CancelInboundPackage;

public sealed record CancelInboundPackageCommand(
    string ClientOrderCode,
    string Reason
) : ICommand;

internal sealed class CancelInboundPackageCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IShippingService shippingService,
    ILogger<CancelInboundPackageCommandHandler> logger)
    : ICommandHandler<CancelInboundPackageCommand>
{
    public async Task<UnitResult<Error>> Handle(
        CancelInboundPackageCommand request,
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

        var shipmentIds = siblings.Select(s => s.Id).ToList();
        var warehouseItems = await db.Set<WarehouseItem>()
            .Where(w => shipmentIds.Contains(w.InboundShipmentId))
            .ToListAsync(cancellationToken);
        var wiByShipment = warehouseItems.ToDictionary(w => w.InboundShipmentId);

        if (!PackageStateResolver.CanCancel(siblings, wiByShipment))
            return Error.Conflict(
                "InboundPackage.NotCancelable",
                "Package cannot be cancelled after it has been received by the warehouse.");

        foreach (var sibling in siblings)
        {
            var result = sibling.Cancel(request.Reason, now);
            if (result.IsFailure)
                return result.Error;
        }

        // Best-effort cancel with carrier for any booked siblings
        foreach (var sibling in siblings.Where(s => s.CarrierTrackingNumber is not null))
        {
            var config = await db.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(c => c.ProviderCode == sibling.ProviderCode && c.IsActive, cancellationToken);
            if (config is null) continue;

            var cancelResult = await shippingService.CancelShipmentAsync(
                sibling.ProviderCode.Id,
                sibling.CarrierTrackingNumber!,
                config,
                cancellationToken);

            if (cancelResult.IsFailure)
            {
                logger.LogWarning(
                    "Carrier cancel failed for {TrackingNumber}: {Error}",
                    sibling.CarrierTrackingNumber, cancelResult.Error.Message);
                return cancelResult.Error;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "InboundPackage {Code} cancelled ({Count} siblings). Reason: {Reason}.",
            code, siblings.Count, request.Reason);

        return UnitResult.Success<Error>();
    }
}
