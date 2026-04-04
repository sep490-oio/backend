using System.Linq.Expressions;
using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class BidMappings
{
    public static BidDto ToDto(this Bid bid, string? bidderDisplayName = null)
    {
        return new BidDto(
            Id: bid.Id.Value,
            AuctionId: bid.AuctionId.Value,
            BidderId: bid.BidderId.Value,
            BidderDisplayName: bidderDisplayName,
            Amount: bid.Amount.ToDto(),
            IsAutoBid: bid.IsAutoBid,
            Status: bid.Status.Id,
            CreatedAt: bid.CreatedAt);
    }
    
    public static readonly SortMappingDefinition BidDtoSortMapping = SortMappingBuilder<BidDto, Bid>
        .Create()
        .Map(x => x.Id, b => b.Id)
        .Map(x => x.AuctionId, b => b.AuctionId)
        .Map(x => x.BidderId, b => b.BidderId)
        .Map(x => x.IsAutoBid, b => b.IsAutoBid)
        .Map(x => x.Amount, b => b.Amount.Amount)
        .Map(x => x.Status, b => b.Status.Id)
        .Map(x => x.CreatedAt, b => b.CreatedAt)
        .Build();
    
    public static readonly SortMappingDefinition MyBidDtoSortMapping = SortMappingBuilder<MyBidDto, Bid>
        .Create()
        .Map(x => x.Id, b => b.Id)
        .Map(x => x.AuctionId, b => b.AuctionId)
        .Map(x => x.ItemTitle, b => b.Auction.Item.Title)
        .Map(x => x.Amount, b => b.Amount.Amount)
        .Map(x => x.Status, b => b.Status.Id)
        .Map(x => x.CurrentPrice, b => b.Auction.Pricing.CurrentAmount)
        .Map(x => x.AuctionStatus, b => b.Auction.Status)
        .Map(x => x.BidPlacedAt, b => b.CreatedAt)
        .Map(x => x.AuctionEndTime, b => b.Auction.Info.EndTime)
        .Build();
}