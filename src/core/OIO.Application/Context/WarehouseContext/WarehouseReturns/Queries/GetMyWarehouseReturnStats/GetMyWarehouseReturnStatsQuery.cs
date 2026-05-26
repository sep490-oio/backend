using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.WarehouseReturns.Queries.GetMyWarehouseReturnStats;

public sealed record GetMyWarehouseReturnStatsQuery : IQuery<SellerWarehouseReturnStatsDto>;
