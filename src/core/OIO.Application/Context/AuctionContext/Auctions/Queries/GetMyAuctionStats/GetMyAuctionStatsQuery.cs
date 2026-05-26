using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Auctions.Queries.GetMyAuctionStats;

public sealed record GetMyAuctionStatsQuery : IQuery<SellerAuctionStatsDto>;
