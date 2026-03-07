using System.Net;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;
[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainModels.BidGrain")]
public struct BidGrain
{
    [Id(0)]
    public Guid Id { get; init; }
    [Id(1)]
    public Guid AuctionId { get; init; }
    [Id(2)]
    public Guid BidderId { get; init; }
    [Id(3)]
    public MoneyGrain Amount { get; init; }
    [Id(4)]
    public bool IsAutoBid { get; init; }      // is_auto_bid
    [Id(5)]
    public Guid? AutoBidId { get; init; }
    [Id(6)]
    public string Status { get; init; }    // status
    [Id(7)]
    public IPAddress? IpAddress { get; init; } // ip_address
    [Id(8)]
    public DateTime CreatedAt { get; init; }  // created_at
    
    public static BidGrain From(Bid bid)
    {
        return new BidGrain
        {
            Id = bid.Id.Value,
            AuctionId = bid.AuctionId.Value,
            BidderId = bid.BidderId.Value,
            Amount = MoneyGrain.From(bid.Amount),
            IsAutoBid = bid.IsAutoBid,
            AutoBidId = bid.AutoBidId?.Value,
            Status = bid.Status.Id,
            IpAddress = bid.IpAddress,
            CreatedAt = bid.CreatedAt
        };
    }
}