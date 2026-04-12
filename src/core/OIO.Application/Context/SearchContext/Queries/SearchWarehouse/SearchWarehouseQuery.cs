using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchWarehouse;

public record SearchWarehouseQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true) : IRequest<PagedList<WarehouseItemDto>>;
