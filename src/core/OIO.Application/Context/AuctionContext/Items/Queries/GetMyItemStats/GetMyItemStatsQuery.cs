using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Items.Queries.GetMyItemStats;

public sealed record GetMyItemStatsQuery : IQuery<SellerItemStatsDto>;
