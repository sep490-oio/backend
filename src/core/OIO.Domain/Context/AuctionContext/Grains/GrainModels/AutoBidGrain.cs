using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;

[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainModels.AutoBidGrain")]
public struct AutoBidGrain
{
    [Id(0)]
    public Guid Id { get; init; }
    [Id(2)]
    public Guid AuctionId { get; init; }
    [Id(3)]
    public Guid BidderId { get; init; }
    [Id(4)]
    public MoneyGrain MaxAmount { get; init; }
    [Id(5)]
    public MoneyGrain CurrentAmount { get; init; }
    [Id(6)]
    public bool IsEnabled { get; init; }
    [Id(7)]
    public MoneyGrain? IncrementAmount { get; init; }
    [Id(8)]
    public string Status { get; init; }
    [Id(9)]
    public int TotalAutoBids { get; init; }
    [Id(10)]
    public DateTime? LastAutoBidAt { get; init; }
    [Id(11)]
    public DateTime CreatedAt { get; init; }
    [Id(12)]
    public DateTime? ModifiedAt { get; init; }
    
    public static AutoBidGrain From(AutoBid autoBid)
    {
        return new AutoBidGrain
        {
            Id = autoBid.Id.Value,
            AuctionId = autoBid.AuctionId.Value,
            BidderId = autoBid.BidderId.Value,
            MaxAmount = MoneyGrain.From(autoBid.Budget.MaxAmount),
            CurrentAmount = MoneyGrain.From(autoBid.Budget.CurrentAmount),
            IsEnabled = autoBid.IsEnabled,
            IncrementAmount = autoBid.Budget.IncrementAmount != null ? MoneyGrain.From(autoBid.Budget.IncrementAmount) : null,
            Status = autoBid.Status.Id,
            TotalAutoBids = autoBid.TotalAutoBids,
            LastAutoBidAt = autoBid.LastAutoBidAt,
            CreatedAt = autoBid.CreatedAt,
            ModifiedAt = autoBid.ModifiedAt
        };
    }
}