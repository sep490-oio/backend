using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchWarehouse;

public class SearchWarehouseQueryHandler(IElasticsearchService searchService)
    : IRequestHandler<SearchWarehouseQuery, PagedList<WarehouseItemDto>>
{
    public async Task<PagedList<WarehouseItemDto>> Handle(SearchWarehouseQuery request, CancellationToken cancellationToken)
    {
        var searchResult = await searchService.SearchAsync<WarehouseItemSearchDocument>(
            request.Query,
            [searchService.WarehouseIndex],
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            null,
            cancellationToken: cancellationToken);

        var mappedResults = searchResult.Results.Select(doc => new WarehouseItemDto(
            Id: Guid.Parse(doc.Id),
            ItemId: Guid.Parse(doc.ItemId),
            InboundShipmentId: Guid.Empty, // Not in ES
            InboundShipmentCode: null,
            StorageLocationId: null,
            StorageLocationLabel: doc.StorageLocation,
            ItemTitle: doc.Title,
            SellerId: !string.IsNullOrEmpty(doc.SellerId) ? Guid.Parse(doc.SellerId) : null,
            SellerName: null,
            ItemImageUrl: null,
            Status: doc.Status,
            ReceivedAt: null,
            CreatedAt: doc.CreatedAt,
            ModifiedAt: doc.ModifiedAt,
            Media: null
        )).ToList();

        return new PagedList<WarehouseItemDto>(
            mappedResults,
            (int)searchResult.Total,
            searchResult.Page,
            searchResult.PageSize);
    }
}
