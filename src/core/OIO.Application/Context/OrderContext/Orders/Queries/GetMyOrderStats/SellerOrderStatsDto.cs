namespace OIO.Application.Context.OrderContext.Orders.Queries.GetMyOrderStats;

public sealed record SellerOrderStatsDto(int OrdersAwaitingShipment, int OrderReturns);
