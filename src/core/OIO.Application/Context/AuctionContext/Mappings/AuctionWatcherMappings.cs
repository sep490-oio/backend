using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class AuctionWatcherMappings
{
    public static readonly SortMappingDefinition MyAuctionWatchlistDtoSortMapping =
        SortMappingBuilder<MyAuctionWatchlistDto, AuctionWatcher>.Create()
            .Map(x => x.AuctionId, x => x.AuctionId)
            .Map(x => x.ItemTitle, x => x.Auction.Item.Title)
            .Map(x => x.CurrentPrice, x => x.Auction.Pricing.CurrentPrice.Amount)
            .Map(x => x.AuctionStatus, x => x.Auction.Status.Id)
            .Map(x => x.BidCount, x => x.Auction.BidCount)
            .Map(x => x.EndTime, x => x.Auction.Info.EndTime)
            .Map(x => x.BidCount, x => x.Auction.BidCount)
            .Map(x => x.WatchedAt, x => x.CreatedAt)
            .Build();
}