using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ScanOrderReturn;

/// <summary>
/// Seller scans the buyer-issued return QR on parcel arrival. Validates the
/// signed token, asserts <see cref="OrderReturnStatus.ReturnInTransit"/> (D4
/// — cannot scan from Approved or anything already received), and flips the
/// return to <see cref="OrderReturnStatus.SellerReceived"/> WITHOUT an
/// evidence guard (D3 — scan is identity-proof only; receipt photos are
/// enforced on <see cref="OrderReturn.Resolve"/> via
/// <c>ConfirmOrderReturnReceivedCommandHandler</c>).
/// </summary>
public sealed record ScanOrderReturnCommand(
    Guid OrderId,
    Guid ReturnId,
    string QrToken) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        ScanOrderReturnCommand.Check()
            .WithOwnerName("ScanOrderReturn")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid()
            .Field(QrToken).NotWhiteSpace();
}

internal sealed class ScanOrderReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IReturnShipmentQrTokenService qrTokenService,
    ILogger<ScanOrderReturnCommandHandler> logger)
    : ICommandHandler<ScanOrderReturnCommand>
{
    private const string OrderReturnKind = "order_return";

    public async Task<UnitResult<Error>> Handle(
        ScanOrderReturnCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden(
                "OrderReturn.ScanForbidden",
                "Only the seller can scan a return shipment.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        // Validate signed QR token.
        var payloadResult = qrTokenService.Validate(request.QrToken);
        if (payloadResult.IsFailure)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                $"QR token is invalid: {payloadResult.Error.Message}");

        var payload = payloadResult.Value;

        if (payload.Kind != OrderReturnKind)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                $"QR token kind '{payload.Kind}' is not valid for order returns.");

        if (payload.ShipmentOrReturnId != order.Return.Id.Value)
            return Error.Validation(
                "qrToken",
                "Scan.InvalidToken",
                "QR token does not match this return.");

        var now = clock.UtcNow;
        if (payload.ExpiresAt <= now)
            return Error.Validation(
                "qrToken",
                "Scan.ExpiredToken",
                "QR token has expired.");

        // D4: scan endpoint MUST only accept returns in ReturnInTransit. Cannot
        // scan a parcel that hasn't shipped (Approved) or that's already received.
        if (order.Return.Status != OrderReturnStatus.ReturnInTransit)
            return Error.Conflict(
                "OrderReturn.InvalidStatus",
                $"Can only scan shipments that are in transit, but return is '{order.Return.Status.Id}'.");

        // D3: MarkSellerReceived carries NO evidence guard — scan is identity-proof only.
        var receiveResult = order.Return.MarkSellerReceived(now);
        if (receiveResult.IsFailure)
            return receiveResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "ScanOrderReturn: OrderReturn {OrderReturnId} flipped to SellerReceived by seller {SellerId}.",
            order.Return.Id.Value, currentUser.UserId.Value);

        return UnitResult.Success<Error>();
    }
}
