using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchItems;

public class SearchItemsQueryHandler(IElasticsearchService searchService)
    : IRequestHandler<SearchItemsQuery, PagedList<PublicItemDto>>
{
    public async Task<PagedList<PublicItemDto>> Handle(SearchItemsQuery request, CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Category)) filters["categoryName.keyword"] = request.Category;
        if (!string.IsNullOrEmpty(request.Status)) filters["status.keyword"] = request.Status;

        var searchResult = await searchService.SearchAsync<ItemSearchDocument>(
            request.Query,
            [searchService.ItemsIndex],
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            filters,
            cancellationToken);

        var mappedResults = searchResult.Results.Select(doc =>
        {
            PublicSellerItemAuctionSummaryDto? auctionSummary = null;
            if (!string.IsNullOrWhiteSpace(doc.AuctionId) && Guid.TryParse(doc.AuctionId, out var auctionIdGuid))
            {
                auctionSummary = new PublicSellerItemAuctionSummaryDto(
                    AuctionId: auctionIdGuid,
                    AuctionStatus: doc.AuctionStatus ?? string.Empty,
                    AuctionType: doc.AuctionType ?? string.Empty,
                    CurrentPrice: doc.AuctionCurrentPrice ?? 0,
                    Currency: doc.AuctionCurrency ?? "VND",
                    StartTime: doc.AuctionStartTime,
                    EndTime: doc.AuctionEndTime);
            }

            return new PublicItemDto(
            Id: Guid.Parse(doc.Id),
            SellerId: Guid.Parse(doc.SellerId),
            SellerName: doc.SellerName,
            CategoryId: !string.IsNullOrEmpty(doc.CategoryId) ? Guid.Parse(doc.CategoryId) : null,
            Title: doc.Title,
            Description: doc.Description,
            Condition: doc.Condition,
            Status: doc.Status,
            Quantity: doc.Quantity,
            Images: doc.Images.Select(img => new ItemMediaDto(
                Id: Guid.Parse(img.Id),
                Url: img.Url,
                PublicId: img.PublicId,
                ResourceType: img.ResourceType,
                IsPrimary: img.IsPrimary,
                SortOrder: img.SortOrder,
                FileName: img.FileName,
                Bytes: img.Bytes,
                Format: img.Format,
                Width: img.Width,
                Height: img.Height,
                DurationSeconds: null
            )).ToList(),
            CreatedAt: doc.CreatedAt,
            Auction: auctionSummary,
            HasLiveAuction: doc.HasLiveAuction
            );
        }).ToList();

        return new PagedList<PublicItemDto>(
            mappedResults,
            (int)searchResult.Total,
            searchResult.Page,
            searchResult.PageSize);
    }
}
