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
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.RejectOrderReturn;

public sealed record RejectOrderReturnCommand(
    Guid OrderId,
    Guid ReturnId,
    string Reason) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        RejectOrderReturnCommand.Check()
            .WithOwnerName("RejectOrderReturn")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class RejectOrderReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISender sender,
    ILogger<RejectOrderReturnCommandHandler> logger)
    : ICommandHandler<RejectOrderReturnCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        RejectOrderReturnCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the seller can reject a return.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        var result = order.Return.Reject(request.Reason, DateTime.UtcNow);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "return_rejected",
                Title: "Yeu cau tra hang bi tu choi",
                Message: $"Yeu cau tra hang cho don {order.OrderNumber.Value} da bi tu choi. Ly do: {request.Reason}",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    returnId = order.Return.Id.Value,
                    reason = request.Reason
                })),
            cancellationToken);

        return order.Return.ToDto();
    }
}
