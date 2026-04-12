using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchOrders;

public record SearchOrdersQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    string? Status = null) : IRequest<PagedList<OrderDto>>;
