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
            StartingPrice: auction.StartingPrice.ToDto(),
            ReservePrice: auction.ReservePrice?.ToDto(),
            BuyNowPrice: auction.BuyNowPrice?.ToDto(),
            CurrentPrice: auction.CurrentPrice.ToDto(),
            BidIncrement: auction.BidIncrement.ToDto(),
            Currency: auction.StartingPrice.Currency.Id,
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
            MinimumBidAmount: auction.GetMinimumBidAmount().ToDto(),
            IsReserveMet: auction.IsReserveMet,
            HasBuyNow: auction.HasBuyNow,
            RemainingTime: auction.RemainingTime(nowUtc),
            IsEndingSoon: auction.IsEndingSoon(nowUtc, extensionThresholdMinutes),
            CreatedAt: auction.CreatedAt);
    }

    public static readonly SortMappingDefinition AuctionListItemDtoSortMapping =
        SortMappingBuilder<AuctionListItemDto, Auction>.Create()
            .Map(x => x.Id, x => x.Id)
            .Map(x => x.ItemTitle, x => x.Item.Id)
            .Map(x => x.CurrentPrice, x => x.CurrentPrice.Amount)
            .Map(x => x.StartingPrice, x => x.StartingPrice.Amount)
            .Map(x => x.BuyNowPrice, x => x.BuyNowPrice!.Amount)
            .Map(x => x.Status, x => x.Status.Id)
            .Map(x => x.BidCount, x => x.BidCount)
            .Map(x => x.WatchCount, x => x.WatchCount)
            .Map(x => x.StartTime, x => x.Duration.StartTime)
            .Map(x => x.EndTime, x => x.Duration.EndTime)
            .Map(x => x.IsFeatured, x => x.IsFeatured)
            .Map(x => x.SellerId, x => x.SellerId)
            .Build();
}