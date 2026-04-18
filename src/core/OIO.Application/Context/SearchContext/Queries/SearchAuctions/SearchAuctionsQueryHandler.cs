using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.Shared.Enums;

namespace OIO.Application.Context.SearchContext.Queries.SearchAuctions;

public sealed class SearchAuctionsQueryHandler(IElasticsearchService searchService) 
    : IRequestHandler<SearchAuctionsQuery, PagedList<AuctionListItemDto>>
{
    public async Task<PagedList<AuctionListItemDto>> Handle(
        SearchAuctionsQuery request, 
        CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string>();
        
        if (!string.IsNullOrEmpty(request.Status))
            filters["status.keyword"] = request.Status;
            
        if (!string.IsNullOrEmpty(request.Category))
            filters["categoryName.keyword"] = request.Category;

        if (!string.IsNullOrEmpty(request.AuctionType))
            filters["auctionType.keyword"] = request.AuctionType;

        if (!string.IsNullOrEmpty(request.Condition))
            filters["condition.keyword"] = request.Condition;

        var (sortField, sortDescending) = MapSortOptions(request.SortBy, request.SortDescending);

        var indices = new[] { searchService.AuctionsIndex };

        var searchResult = await searchService.SearchAsync<AuctionSearchDocument>(
            request.Query,
            indices,
            request.Page,
            request.PageSize,
            sortField,
            sortDescending,
            filters,
            request.MinPrice,
            request.MaxPrice,
            cancellationToken);

        var nowUtc = DateTime.UtcNow;
        var mappedResults = searchResult.Results.Select(doc =>
        {
            var symbol = Currency.GetSymbol(doc.Currency);
            
            return new AuctionListItemDto(
                Id: Guid.Parse(doc.Id),
                ItemTitle: doc.Title,
                PrimaryImageUrl: doc.ThumbnailUrl,
                CurrentPrice: new MoneyDto(doc.CurrentPrice, doc.Currency, symbol),
                StartingPrice: new MoneyDto(doc.StartingPrice, doc.Currency, symbol),
                BuyNowPrice: doc.BuyNowPrice.HasValue ? new MoneyDto(doc.BuyNowPrice.Value, doc.Currency, symbol) : null,
                IsBuyNowReserved: doc.IsBuyNowReserved,
                BuyNowReservedUntil: null, // Not easily stored as a flag, usually false if not reserved
                Currency: doc.Currency,
                Status: doc.Status,
                BidCount: doc.BidCount,
                WatchCount: doc.WatchCount,
                ViewCount: doc.ViewCount,
                StartTime: doc.StartTime,
                EndTime: doc.EndTime,
                RemainingTime: doc.EndTime.HasValue ? doc.EndTime.Value - nowUtc : TimeSpan.Zero,
                CreatedAt: doc.CreatedAt,
                IsEndingSoon: doc.EndTime.HasValue && (doc.EndTime.Value - nowUtc).TotalHours < 1,
                IsFeatured: doc.IsFeatured,
                SellerId: Guid.Parse(doc.SellerId),
                ItemStatus: doc.ItemStatus,
                AuctionType: doc.AuctionType
            );
        }).ToList();

        return new PagedList<AuctionListItemDto>(
            mappedResults,
            (int)searchResult.Total,
            searchResult.Page,
            searchResult.PageSize);
    }

    private static (string? Field, bool Descending) MapSortOptions(string? sortBy, bool defaultDescending)
    {
        if (string.IsNullOrEmpty(sortBy)) return (null, defaultDescending);

        return sortBy.ToLowerInvariant() switch
        {
            "ending_soon" => ("endTime", false),
            "price_asc" => ("currentPrice", false),
            "price_desc" => ("currentPrice", true),
            "most_bids" => ("bidCount", true),
            "newest" => ("createdAt", true),
            _ => (sortBy, defaultDescending)
        };
    }
}
