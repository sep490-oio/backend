using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.ConfirmWarehouseReturnReceipt;

/// <summary>
/// Seller confirms they received a returned item that was routed back to them.
/// Transitions Delivered → Closed.
/// </summary>
public sealed record ConfirmWarehouseReturnReceiptCommand(Guid ShipmentId)
    : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfirmWarehouseReturnReceiptCommand.Check()
            .WithOwnerName("ConfirmWarehouseReturnReceipt")
            .Field(ShipmentId)
            .NotEmptyGuid();
    }
}

internal sealed class ConfirmWarehouseReturnReceiptCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    ILogger<ConfirmWarehouseReturnReceiptCommandHandler> logger)
    : ICommandHandler<ConfirmWarehouseReturnReceiptCommand>
{
    public async Task<UnitResult<Error>> Handle(
        ConfirmWarehouseReturnReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        if (shipment.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "WarehouseToSellerShipment.NotOwner",
                "Only the seller receiving the shipment can confirm receipt.");

        var now = clock.UtcNow;
        var confirmResult = shipment.ConfirmBySeller(now);
        if (confirmResult.IsFailure)
            return confirmResult.Error;

        dbContext.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "WarehouseToSellerShipment {ShipmentId} confirmed received by seller {SellerId}.",
            shipment.Id.Value, currentUser.UserId.Value);

        return UnitResult.Success<Error>();
    }
}
