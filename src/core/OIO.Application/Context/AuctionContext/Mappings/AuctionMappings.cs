using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class AuctionMappings
{
    public static AuctionListItemDto ToListItemDto(this Auction auction, DateTime nowUtc, TimeSpan extensionThresholdMinutes)
    {
        var activeReservation = auction.GetActiveBuyNowReservation(nowUtc);
        var primaryImageUrl = auction.Item.Media
            .Where(img => img.IsPrimary)
            .OrderBy(img => img.SortOrder)
            .Select(img => img.Info.SecureUrl)
            .FirstOrDefault();

        return new AuctionListItemDto(
            Id: auction.Id.Value,
            ItemTitle: auction.Item.Title.Value,
            PrimaryImageUrl: primaryImageUrl,
            CurrentPrice: auction.Pricing.CurrentPrice.ToDto(),
            StartingPrice: auction.Pricing.StartingPrice.ToDto(),
            BuyNowPrice: auction.Pricing.BuyNowPrice?.ToDto(),
            IsBuyNowReserved: activeReservation is not null,
            BuyNowReservedUntil: activeReservation?.ExpiresAt,
            Currency: auction.Pricing.Currency.Id,
            Status: auction.Status.Id,
            BidCount: auction.BidCount,
            WatchCount: auction.WatchCount,
            ViewCount: auction.ViewCount,
            StartTime: auction.Info?.StartTime,
            EndTime: auction.Info?.EndTime,
            RemainingTime: auction.Info?.RemainingTime(nowUtc),
            IsEndingSoon: auction.Info is not null ? auction.IsEndingSoon(nowUtc, extensionThresholdMinutes) : null,
            CreatedAt: auction.CreatedAt,
            IsFeatured: auction.IsFeatured,
            SellerId: auction.Item.SellerId.Value,
            ItemStatus: auction.Item.Status.Id);
    }

    public static AuctionDto ToDto(this Auction auction, DateTime nowUtc, TimeSpan extensionThresholdMinutes)
    {
        var activeReservation = auction.GetActiveBuyNowReservation(nowUtc);

        return new AuctionDto(
            Id: auction.Id.Value,
            ItemId: auction.ItemId.Value,
            SellerId: auction.Item.SellerId.Value,
            AuctionType: auction.AuctionType?.Id ?? AuctionType.Regular.Id,
            StartingPrice: auction.Pricing.StartingPrice.ToDto(),
            ReservePrice: auction.Pricing.ReservePrice?.ToDto(),
            BuyNowPrice: auction.Pricing.BuyNowPrice?.ToDto(),
            CurrentPrice: auction.Pricing.CurrentPrice.ToDto(),
            BidIncrement: auction.Pricing.BidIncrement.ToDto(),
            Currency: auction.Pricing.Currency.Id,
            StartTime: auction.Info?.StartTime,
            EndTime: auction.Info?.EndTime,
            ActualEndTime: auction.ActualEndTime,
            QualificationStartAt: auction.Info?.Qualification?.StartTime,
            QualificationEndAt: auction.Info?.Qualification?.EndTime,
            Status: auction.Status.Id,
            CurrentWinnerId: auction.WinnerId?.Value,
            AutoExtend: auction.Info?.AutoExtend ?? false,
            ExtensionMinutes: auction.Info?.ExtensionMinutes ?? 0,
            ExtensionCount: auction.Info?.ExtensionCount ?? 0,
            AssignedAdminId: auction.AssignedAdminId?.Value,
            AssignedAt: auction.AssignedAt,
            IsFeatured: auction.IsFeatured,
            Priority: auction.Priority?.Score ?? 0,
            PriorityReason: auction.Priority?.Reason ?? "{}",
            VerifyByPlatform: auction.VerifyByPlatform,
            RejectionCount: auction.RejectionCount,
            ViewCount: auction.ViewCount,
            BidCount: auction.BidCount,
            WatchCount: auction.WatchCount,
            MinimumBidAmount: auction.GetMinimumBidAmount().ToDto(),
            IsReserveMet: auction.Pricing.ReserveMet,
            HasBuyNow: auction.Pricing.HasBuyNowPrice,
            IsBuyNowReserved: activeReservation is not null,
            BuyNowReservedUntil: activeReservation?.ExpiresAt,
            RemainingTime: auction.Info?.RemainingTime(nowUtc) ?? TimeSpan.Zero,
            IsEndingSoon: auction.IsEndingSoon(nowUtc, extensionThresholdMinutes),
            CreatedAt: auction.CreatedAt);
    }

    public static readonly SortMappingDefinition AuctionListItemDtoSortMapping =
        SortMappingBuilder<AuctionListItemDto, Auction>.Create()
            .Map(x => x.Id, x => x.Id)
            .Map(x => x.ItemTitle, x => x.Item.Id)
            .Map(x => x.CurrentPrice, x => x.Pricing.CurrentAmount)
            .Map(x => x.StartingPrice, x => x.Pricing.StartingAmount)
            .Map(x => x.BuyNowPrice, x => x.Pricing.BuyNowAmount)
            .Map(x => x.Status, x => x.Status.Id)
            .Map(x => x.BidCount, x => x.BidCount)
            .Map(x => x.WatchCount, x => x.WatchCount)
            .Map(x => x.ViewCount, x => x.ViewCount)
            .Map(x => x.StartTime, x => x.Info!.StartTime)
            .Map(x => x.EndTime, x => x.Info!.EndTime)
            .Map(x => x.IsFeatured, x => x.IsFeatured)
            .Map(x => x.SellerId, x => x.Item.SellerId)
            .Map(x => x.CreatedAt, x => x.CreatedAt)
            .Build();
}
