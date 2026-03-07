using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class AuctionMappings
{
    public static AuctionDto ToDto(this Auction auction, DateTime nowUtc, TimeSpan extensionThresholdMinutes)
    {
        return new AuctionDto(
            Id: auction.Id.Value,
            ItemId: auction.ItemId.Value,
            SellerId: auction.SellerId.Value,
            StartingPrice: auction.StartingPrice.Amount,
            ReservePrice: auction.ReservePrice?.Amount,
            BuyNowPrice: auction.BuyNowPrice?.Amount,
            CurrentPrice: auction.CurrentPrice.Amount,
            BidIncrement: auction.BidIncrement.Amount,
            Currency: auction.Currency,
            StartTime: auction.Duration.StartTime,
            EndTime: auction.Duration.EndTime,
            ActualEndTime: auction.ActualEndTime,
            Status: auction.Status.Id,
            CurrentWinnerId: auction.CurrentWinnerId?.Value,
            AutoExtend: auction.AutoExtend,
            ExtensionMinutes: auction.ExtensionMinutes,
            IsFeatured: auction.IsFeatured,
            ViewCount: auction.ViewCount,
            BidCount: auction.BidCount,
            WatchCount: auction.WatchCount,
            MinimumBidAmount: auction.GetMinimumBidAmount().Amount,
            IsReserveMet: auction.IsReserveMet,
            HasBuyNow: auction.HasBuyNow,
            RemainingTime: auction.RemainingTime(nowUtc),
            IsEndingSoon: auction.IsEndingSoon(nowUtc, extensionThresholdMinutes),
            CreatedAt: auction.CreatedAt);
    }
    
    public static readonly SortMappingDefinition<AuctionListItemDto, Auction> SortMapping = new()
    {
        Mappings =
        [
            new SortMapping(nameof(AuctionListItemDto.CurrentPrice), nameof(Auction.CurrentPrice)),
            new SortMapping(nameof(AuctionListItemDto.StartingPrice), nameof(Auction.StartingPrice)),
            new SortMapping(nameof(AuctionListItemDto.BuyNowPrice), nameof(Auction.BuyNowPrice)),
            new SortMapping(nameof(AuctionListItemDto.Status), nameof(Auction.Status)),
            new SortMapping(nameof(AuctionListItemDto.Currency), nameof(Auction.Currency)),
            new SortMapping(nameof(AuctionListItemDto.BidCount), nameof(Auction.BidCount)),
            new SortMapping(nameof(AuctionListItemDto.WatchCount), nameof(Auction.WatchCount)),
            new SortMapping(nameof(AuctionListItemDto.StartTime), nameof(Auction.Duration.StartTime)),
            new SortMapping(nameof(AuctionListItemDto.EndTime), nameof(Auction.Duration.EndTime)),
            new SortMapping(nameof(AuctionListItemDto.IsFeatured), nameof(Auction.IsFeatured)),
            new SortMapping(nameof(AuctionListItemDto.SellerId), nameof(Auction.SellerId)),
            new SortMapping(nameof(AuctionListItemDto.Id), nameof(Auction.Id)),
            // new SortMapping(nameof(AuctionListItemDto.RemainingTime), nameof(Auction.RemainingTime)),
            // new SortMapping(nameof(AuctionListItemDto.IsEndingSoon), nameof(Auction.IsEndingSoon)),
        ]
    };
}