using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Orders.Queries.GetMyOrderStats;

public sealed record GetMyOrderStatsQuery : IQuery<SellerOrderStatsDto>;
