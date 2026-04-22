using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ShipOrderReturn;

public sealed record ShipOrderReturnCommand(
    Guid OrderId,
    Guid ReturnId,
    string ProviderCode,
    string TrackingNumber) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ShipOrderReturnCommand.Check()
            .WithOwnerName("ShipOrderReturn")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid()
            .Field(ProviderCode).NotWhiteSpace()
            .Field(TrackingNumber).NotWhiteSpace();
}

internal sealed class ShipOrderReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IReturnShipmentQrTokenService qrTokenService,
    ISender sender,
    ILogger<ShipOrderReturnCommandHandler> logger)
    : ICommandHandler<ShipOrderReturnCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        ShipOrderReturnCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
                .ThenInclude(r => r!.Evidence)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.BuyerId != currentUser.UserId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the buyer can ship a return.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        var now = clock.UtcNow;
        var result = order.Return.MarkReturnShipped(
            request.ProviderCode,
            request.TrackingNumber,
            now,
            now);

        if (result.IsFailure)
            return result.Error;

        // C6: issue a signed return-scoped QR token so the seller can scan on arrival.
        // Mirrors the outbound flow pattern: token is stamped on the aggregate and
        // the FE reads it from the response DTO to render the QR.
        var qrToken = qrTokenService.Issue(
            kind:              "order_return",
            shipmentOrReturnId: order.Return.Id.Value,
            issuedAt:          now,
            expiresAt:         now.AddDays(30));

        var qrResult = order.Return.IssueReturnQr(qrToken, now);
        if (qrResult.IsFailure)
            return qrResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.SellerId.Value,
                NotificationType: "order",
                EventType: "return_shipped",
                Title: "Buyer da gui hang hoan lai",
                Message:
                    $"Buyer da gui hang hoan lai cho don {order.OrderNumber.Value}. " +
                    $"Ma van don: {request.TrackingNumber}.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    returnId = order.Return.Id.Value,
                    providerCode = request.ProviderCode,
                    trackingNumber = request.TrackingNumber
                })),
            cancellationToken);

        return order.Return.ToDto();
    }
}
