using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class BidMappings
{
    public static BidDto ToDto(this Bid bid)
    {
        return new BidDto(
            Id: bid.Id.Value,
            AuctionId: bid.AuctionId.Value,
            BidderId: bid.BidderId.Value,
            Amount: bid.Amount.Amount,
            IsAutoBid: bid.IsAutoBid,
            Status: bid.Status.Id,
            CreatedAt: bid.CreatedAt);
    }
    
    public static readonly SortMappingDefinition<MyBidDto, Bid> SortMapping = new()
    {
        Mappings =
        [
            new SortMapping(nameof(MyBidDto.BidId), $"{nameof(Bid)}.{nameof(Bid.Id)}"),
            new SortMapping(nameof(MyBidDto.BidAmount), nameof(Bid.Amount)),
            new SortMapping(nameof(MyBidDto.BidStatus), nameof(Bid.Status)),
            new SortMapping(nameof(MyBidDto.BidPlacedAt), nameof(Bid.CreatedAt)),
        ]
    };
}