namespace OIO.Application.Context.AuctionContext.Auctions.Queries.GetMyAuctionStats;

public sealed record SellerAuctionStatsDto(int TotalAuctions, int ActiveAuctions, int DraftAuctions, int EndedAuctions);
