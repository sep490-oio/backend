using MediatR;
using OIO.Application.Abstractions.Search;

namespace OIO.Application.Context.SearchContext.Queries.GetSuggestions;

public record GetSearchSuggestionsQuery(string Query, string? IndexName = null) : IRequest<List<string>>;

public class GetSearchSuggestionsQueryHandler(IElasticsearchService searchService) 
    : IRequestHandler<GetSearchSuggestionsQuery, List<string>>
{
    public async Task<List<string>> Handle(GetSearchSuggestionsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return [];
        }

        string[] indices = !string.IsNullOrEmpty(request.IndexName)
            ? [request.IndexName]
            : [searchService.ItemsIndex, searchService.AuctionsIndex];

        return await searchService.GetSuggestionsAsync(request.Query, indices, cancellationToken);
    }
}
