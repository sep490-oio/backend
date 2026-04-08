using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Services;

/// <summary>
/// Shared helper that transitions an <see cref="Order"/> to Delivered and
/// starts the return-decision window. Used by the seller self-ship command,
/// the outbound-shipment delivered event handler, and the buyer proof-of-
/// delivery flow so buyer protection logic stays in one place.
/// </summary>
public interface IOrderDeliveryService
{
    Task<Result<OrderDto, Error>> MarkAsDeliveredAsync(
        Order order,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}

internal sealed class OrderDeliveryService(IRuntimeSettings runtimeSettings) : IOrderDeliveryService
{
    public Task<Result<OrderDto, Error>> MarkAsDeliveredAsync(
        Order order,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        // Idempotent no-op: the order is already at or past Delivered. We keep
        // the existing DecisionWindowEndsAt untouched so the buyer protection
        // window isn't silently extended by repeated calls.
        if (IsAtOrPastDelivered(order.Status))
            return Task.FromResult(Result.Success<OrderDto, Error>(order.ToDto()));

        var decisionWindowDays = runtimeSettings.Order.ReturnDecisionWindowDays;
        var result = order.MarkAsDelivered(nowUtc, nowUtc.AddDays(decisionWindowDays), nowUtc);
        if (result.IsFailure)
            return Task.FromResult(Result.Failure<OrderDto, Error>(result.Error));

        return Task.FromResult(Result.Success<OrderDto, Error>(order.ToDto()));
    }

    private static bool IsAtOrPastDelivered(OrderStatus status) =>
        status == OrderStatus.Delivered ||
        status == OrderStatus.Completed ||
        status == OrderStatus.Disputed ||
        status == OrderStatus.Refunded;
}
