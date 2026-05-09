using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.Admin.AdminOverrideOrderStatus;

public sealed record AdminOverrideOrderStatusCommand(
    Guid OrderId,
    string NewStatus,
    string Reason) : ICommand;

internal sealed class AdminOverrideOrderStatusCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<AdminOverrideOrderStatusCommandHandler> logger)
    : ICommandHandler<AdminOverrideOrderStatusCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminOverrideOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;

        // Validate target status by matching against known static OrderStatus fields
        var allStatuses = new[]
        {
            OrderStatus.PendingPayment, OrderStatus.Paid, OrderStatus.Processing,
            OrderStatus.PickedUp, OrderStatus.OnDelivering, OrderStatus.Shipped,
            OrderStatus.Delivered, OrderStatus.Completed, OrderStatus.Cancelled,
            OrderStatus.Refunded, OrderStatus.Disputed
        };
        var newStatus = allStatuses.FirstOrDefault(s => s.Id == request.NewStatus);
        if (newStatus is null)
            return Error.Validation(
                "newStatus",
                "Order.InvalidStatus",
                $"'{request.NewStatus}' is not a valid order status.");

        var order = await dbContext.GetByIdAsync<Order, OrderId>(
            OrderId.From(request.OrderId),
            cancellationToken: cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order not found.");

        var overrideResult = order.AdminOverrideStatus(newStatus, request.Reason, nowUtc);
        if (overrideResult.IsFailure)
            return overrideResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Admin status override on order {OrderId} ({OrderNumber}): → {NewStatus}. Reason: {Reason}",
            request.OrderId, order.OrderNumber.Value, request.NewStatus, request.Reason);

        return UnitResult.Success<Error>();
    }
}
