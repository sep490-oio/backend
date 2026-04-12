using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.AuctionContext.DTOs;

namespace OIO.Application.Context.SearchContext.Queries.SearchAuctions;

public record SearchAuctionsQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Status = null,
    string? Category = null) : IRequest<PagedList<AuctionListItemDto>>;
