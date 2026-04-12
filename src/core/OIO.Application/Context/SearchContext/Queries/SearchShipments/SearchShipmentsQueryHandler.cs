using MediatR;
using OIO.Application.Abstractions.Search;

namespace OIO.Application.Context.SearchContext.Queries.SearchShipments;

public class SearchShipmentsQueryHandler(IElasticsearchService searchService)
    : IRequestHandler<SearchShipmentsQuery, SearchResponseDto<ShipmentSearchDocument>>
{
    public async Task<SearchResponseDto<ShipmentSearchDocument>> Handle(SearchShipmentsQuery request, CancellationToken cancellationToken)
    {
        var filters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(request.Type)) filters["shipmentType.keyword"] = request.Type;
        if (!string.IsNullOrEmpty(request.Provider)) filters["providerCode.keyword"] = request.Provider;

        return await searchService.SearchAsync<ShipmentSearchDocument>(
            request.Query,
            [searchService.ShipmentsIndex],
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            filters,
            cancellationToken);
    }
}
