using MediatR;
using OIO.Application.Abstractions.Search;

namespace OIO.Application.Context.SearchContext.Queries.SearchShipments;

public record SearchShipmentsQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    string? Type = null,
    string? Provider = null) : IRequest<SearchResponseDto<ShipmentSearchDocument>>;
