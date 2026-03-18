using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ConfirmOrderReturnReceived;

public sealed record ConfirmOrderReturnReceivedCommand(
    Guid OrderId,
    Guid ReturnId) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ConfirmOrderReturnReceivedCommand.Check()
            .WithOwnerName("ConfirmOrderReturnReceived")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid();
}

internal sealed class ConfirmOrderReturnReceivedCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    EscrowSettlementService settlementService,
    ISender sender,
    ILogger<ConfirmOrderReturnReceivedCommandHandler> logger)
    : ICommandHandler<ConfirmOrderReturnReceivedCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        ConfirmOrderReturnReceivedCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the seller can confirm returned goods.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        var receiveResult = order.Return.MarkSellerReceived(DateTime.UtcNow);
        if (receiveResult.IsFailure)
            return receiveResult.Error;

        var refundResult = await settlementService.RefundBuyerAsync(
            order,
            partialAmount: null,
            reason: "Return received by seller",
            actorId: currentUser.UserId,
            cancellationToken: cancellationToken);

        if (refundResult.IsFailure)
            return refundResult.Error;

        var resolveResult = order.Return.Resolve(DateTime.UtcNow);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "refund_completed",
                Title: "Hoan tien da hoan tat",
                Message: $"Khoan tien giu cho don {order.OrderNumber.Value} da duoc hoan cho ban.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value),
            cancellationToken);

        return order.Return.ToDto();
    }
}
