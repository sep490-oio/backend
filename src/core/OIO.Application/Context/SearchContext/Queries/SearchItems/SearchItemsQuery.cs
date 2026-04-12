using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchItems;

public record SearchItemsQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    string? Status = null,
    string? Category = null) : IRequest<PagedList<PublicItemDto>>;
