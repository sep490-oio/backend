namespace OIO.Application.Abstractions.Search;

public interface IElasticsearchService
{
    Task IndexDocumentAsync<T>(T document, string indexName, bool bypassCacheInvalidation = false, CancellationToken cancellationToken = default) where T : class;
    Task UpdateDocumentAsync<T>(T document, string indexName, bool bypassCacheInvalidation = false, CancellationToken cancellationToken = default) where T : class;
    Task DeleteDocumentAsync(string id, string indexName, bool bypassCacheInvalidation = false, CancellationToken cancellationToken = default);

    string AuctionsIndex { get; }
    string ItemsIndex { get; }
    string UsersIndex { get; }
    string OrdersIndex { get; }
    string ShipmentsIndex { get; }
    string WarehouseIndex { get; }
    
    Task<SearchResponseDto<T>> SearchAsync<T>(
        string query, 
        string[] indices, 
        int page = 1, 
        int pageSize = 10,
        string? sortBy = null,
        bool sortDescending = true,
        Dictionary<string, string>? filters = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default) where T : class;

    Task<List<string>> GetSuggestionsAsync(
        string query,
        string[] indices,
        CancellationToken cancellationToken = default);
    Task<List<string>> GetAllIdsAsync(string index, CancellationToken cancellationToken = default);

    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    Task RecreateIndicesAsync(CancellationToken cancellationToken = default);
}

public class SearchResponseDto<T>
{
    public IReadOnlyCollection<T> Results { get; init; } = [];
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public List<FacetDto> Facets { get; init; } = [];
}

public record FacetDto(string Name, List<FacetBucketDto> Buckets);
public record FacetBucketDto(string Key, long Count);
