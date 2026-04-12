using MediatR;
using OIO.Application.Abstractions.Search;

namespace OIO.Application.Context.SearchContext.Queries.GlobalSearch;

public record GlobalSearchQuery(
    string Query,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    bool SortDescending = true,
    Dictionary<string, string>? Filters = null) : IRequest<SearchResponseDto<BaseSearchDocument>>;

public class GlobalSearchQueryHandler(IElasticsearchService searchService) 
    : IRequestHandler<GlobalSearchQuery, SearchResponseDto<BaseSearchDocument>>
{
    public async Task<SearchResponseDto<BaseSearchDocument>> Handle(
        GlobalSearchQuery request, 
        CancellationToken cancellationToken)
    {
        var indices = new[] { searchService.ItemsIndex, searchService.AuctionsIndex };

        return await searchService.SearchAsync<BaseSearchDocument>(
            request.Query,
            indices,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.SortDescending,
            request.Filters,
            cancellationToken);
    }
}
