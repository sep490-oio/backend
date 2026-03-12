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
            SellerId: auction.Item.SellerId.Value,
            StartingPrice: auction.Pricing.StartingPrice.ToDto(),
            ReservePrice: auction.Pricing.ReservePrice?.ToDto(),
            BuyNowPrice: auction.Pricing.BuyNowPrice?.ToDto(),
            CurrentPrice: auction.Pricing.CurrentPrice.ToDto(),
            BidIncrement: auction.Pricing.BidIncrement.ToDto(),
            Currency: auction.Pricing.Currency.Id,
            StartTime: auction.Info.StartTime,
            EndTime: auction.Info.EndTime,
            ActualEndTime: auction.ActualEndTime,
            Status: auction.Status.Id,
            CurrentWinnerId: auction.WinnerId?.Value,
            AutoExtend: auction.Info.AutoExtend,
            ExtensionMinutes: auction.Info.ExtensionMinutes,
            IsFeatured: auction.IsFeatured,
            ViewCount: auction.ViewCount,
            BidCount: auction.BidCount,
            WatchCount: auction.WatchCount,
            MinimumBidAmount: auction.GetMinimumBidAmount().ToDto(),
            IsReserveMet: auction.Pricing.ReserveMet,
            HasBuyNow: auction.Pricing.HasBuyNowPrice,
            RemainingTime: auction.Info.RemainingTime(nowUtc),
            IsEndingSoon: auction.IsEndingSoon(nowUtc, extensionThresholdMinutes),
            CreatedAt: auction.CreatedAt);
    }

    public static readonly SortMappingDefinition AuctionListItemDtoSortMapping =
        SortMappingBuilder<AuctionListItemDto, Auction>.Create()
            .Map(x => x.Id, x => x.Id)
            .Map(x => x.ItemTitle, x => x.Item.Id)
            .Map(x => x.CurrentPrice, x => x.Pricing.CurrentPrice.Amount)
            .Map(x => x.StartingPrice, x => x.Pricing.StartingPrice.Amount)
            .Map(x => x.BuyNowPrice, x => x.Pricing.BuyNowPrice!.Amount)
            .Map(x => x.Status, x => x.Status.Id)
            .Map(x => x.BidCount, x => x.BidCount)
            .Map(x => x.WatchCount, x => x.WatchCount)
            .Map(x => x.StartTime, x => x.Info.StartTime)
            .Map(x => x.EndTime, x => x.Info.EndTime)
            .Map(x => x.IsFeatured, x => x.IsFeatured)
            .Map(x => x.SellerId, x => x.Item.SellerId)
            .Build();
}