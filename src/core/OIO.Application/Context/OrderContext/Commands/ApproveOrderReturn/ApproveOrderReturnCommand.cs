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

namespace OIO.Application.Context.OrderContext.Commands.ApproveOrderReturn;

public sealed record ApproveOrderReturnCommand(
    Guid OrderId,
    Guid ReturnId,
    string? Notes) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ApproveOrderReturnCommand.Check()
            .WithOwnerName("ApproveOrderReturn")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid();
}

internal sealed class ApproveOrderReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISender sender,
    ILogger<ApproveOrderReturnCommandHandler> logger)
    : ICommandHandler<ApproveOrderReturnCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        ApproveOrderReturnCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden("Order.ReturnForbidden", "Only the seller can approve a return.");

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        var result = order.Return.Approve(request.Notes, DateTime.UtcNow);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.BuyerId.Value,
                NotificationType: "order",
                EventType: "return_approved",
                Title: "Yeu cau tra hang da duoc chap nhan",
                Message: $"Yeu cau tra hang cho don {order.OrderNumber.Value} da duoc chap nhan. Vui long gui hang hoan lai.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    returnId = order.Return.Id.Value
                })),
            cancellationToken);

        return order.Return.ToDto();
    }
}
