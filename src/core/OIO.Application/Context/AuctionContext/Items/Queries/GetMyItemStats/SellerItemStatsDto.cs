namespace OIO.Application.Context.AuctionContext.Items.Queries.GetMyItemStats;

public sealed record SellerItemStatsDto(int TotalItems, int PendingReviewItems, int RejectedItems);
