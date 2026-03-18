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

namespace OIO.Application.Context.OrderContext.Commands.RequestOrderReturn;

public sealed record RequestOrderReturnCommand(
    Guid OrderId,
    string ReasonCode,
    string? Description) : ICommand<OrderReturnDto>, IHasValidate
{
    public ViolationsError Validate() =>
        RequestOrderReturnCommand.Check()
            .WithOwnerName("RequestOrderReturn")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReasonCode).NotWhiteSpace();
}

internal sealed class RequestOrderReturnCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISender sender,
    ILogger<RequestOrderReturnCommandHandler> logger)
    : ICommandHandler<RequestOrderReturnCommand, OrderReturnDto>
{
    public async Task<Result<OrderReturnDto, Error>> Handle(
        RequestOrderReturnCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);
        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        var result = order.RequestReturn(
            currentUser.UserId,
            request.ReasonCode,
            request.Description,
            DateTime.UtcNow);

        if (result.IsFailure)
            return result.Error;

        dbContext.Insert(result.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: order.SellerId.Value,
                NotificationType: "order",
                EventType: "return_requested",
                Title: "Buyer da yeu cau tra hang",
                Message: $"Don hang {order.OrderNumber.Value} da co yeu cau tra hang.",
                Priority: NotificationPriority.High,
                EntityType: "Order",
                EntityId: order.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    orderId = order.Id.Value,
                    returnId = result.Value.Id.Value,
                    reasonCode = request.ReasonCode
                })),
            cancellationToken);

        return result.Value.ToDto();
    }
}
