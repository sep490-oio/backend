using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ConfirmSellerOrder;

/// <summary>
/// Seller confirms a paid order and begins fulfillment.
/// Transitions OrderStatus: Paid → Processing.
///
/// Authorization: only the order's seller may invoke.
/// Precondition: order must be in <c>paid</c> status.
/// </summary>
public sealed record ConfirmSellerOrderCommand(Guid OrderId)
    : ICommand<OrderDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ConfirmSellerOrderCommand.Check()
            .WithOwnerName("ConfirmSellerOrder")
            .Field(OrderId).NotEmptyGuid();
}

internal sealed class ConfirmSellerOrderCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<ConfirmSellerOrderCommand, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        ConfirmSellerOrderCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);

        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        if (order.SellerId != currentUser.UserId)
            return Error.Forbidden("Order.ConfirmForbidden", "Only the seller can confirm this order.");

        var result = order.ConfirmBySeller(clock.UtcNow);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
