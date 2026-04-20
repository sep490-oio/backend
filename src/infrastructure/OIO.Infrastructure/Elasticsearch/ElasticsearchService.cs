using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Transport;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Search;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Elasticsearch;

public class ElasticsearchService : IElasticsearchService
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchSettings _settings;
    private readonly HybridCache _cache;

    public ElasticsearchService(IOptions<ElasticsearchSettings> settings, HybridCache cache)
    {
        _settings = settings.Value;
        _cache = cache;
        
        var clientSettings = new ElasticsearchClientSettings(new Uri(_settings.Endpoint))
            .Authentication(new ApiKey(_settings.ApiKey))
            .DefaultIndex(_settings.AuctionsIndex)
            .DisableDirectStreaming(true);

        _client = new ElasticsearchClient(clientSettings);
    }

    public string AuctionsIndex => _settings.AuctionsIndex;
    public string ItemsIndex => _settings.ItemsIndex;
    public string UsersIndex => _settings.UsersIndex;
    public string OrdersIndex => _settings.OrdersIndex;
    public string ShipmentsIndex => _settings.ShipmentsIndex;
    public string WarehouseIndex => _settings.WarehouseIndex;

    public async Task IndexDocumentAsync<T>(T document, string indexName, CancellationToken cancellationToken = default) where T : class
    {
        var response = await _client.IndexAsync(document, (IndexName)indexName, cancellationToken);
        if (!response.IsSuccess())
        {
            throw new Exception($"Failed to index document: {response.ElasticsearchServerError?.Error.Reason}");
        }
    }

    public async Task UpdateDocumentAsync<T>(T document, string indexName, CancellationToken cancellationToken = default) where T : class
    {
        // In ES, IndexAsync handles both create and replace (upsert) if ID is provided.
        // For specific updates we could use UpdateAsync, but here we usually push the full doc.
        await IndexDocumentAsync(document, indexName, cancellationToken);
    }

    public async Task DeleteDocumentAsync(string id, string indexName, CancellationToken cancellationToken = default)
    {
        var response = await _client.DeleteAsync(index: (IndexName)indexName, id: (Id)id, cancellationToken: cancellationToken);
        if (!response.IsSuccess() && response.ElasticsearchServerError?.Status != 404)
        {
            throw new Exception($"Failed to delete document: {response.ElasticsearchServerError?.Error.Reason}");
        }
    }

    public async Task<SearchResponseDto<T>> SearchAsync<T>(
        string query,
        string[] indices,
        int page = 1,
        int pageSize = 10,
        string? sortBy = null,
        bool sortDescending = true,
        Dictionary<string, string>? filters = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cacheKey = GetCacheKey("search", query, indices, page, pageSize, sortBy, sortDescending, filters, minPrice, maxPrice);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await ExecuteSearchAsync<T>(query, indices, page, pageSize, sortBy, sortDescending, filters, minPrice, maxPrice, ct),
            cancellationToken: cancellationToken);
    }

    private async Task<SearchResponseDto<T>> ExecuteSearchAsync<T>(
        string query,
        string[] indices,
        int page,
        int pageSize,
        string? sortBy,
        bool sortDescending,
        Dictionary<string, string>? filters,
        decimal? minPrice,
        decimal? maxPrice,
        CancellationToken cancellationToken) where T : class
    {
        var from = (page - 1) * pageSize;

        var response = await _client.SearchAsync<T>(s =>
        {
            s.Indices(indices)
             .From(from)
             .Size(pageSize)
             .Query(q => q
                .Bool(b =>
                {
                    b.Must(m =>
                    {
                        if (query == "*" || string.IsNullOrWhiteSpace(query))
                        {
                            m.MatchAll(new MatchAllQuery());
                        }
                        else
                        {
                            m.Bool(b => b
                                .Should(
                                    sh => sh.MultiMatch(mm => mm
                                        .Query(query)
                                        .Fields(new[] { "title^3", "displayName^3", "description", "categoryName", "id", "sellerName", "entityType" })
                                        .Fuzziness(new Fuzziness("AUTO"))
                                    ),
                                    sh => sh.MatchPhrasePrefix(mpp => mpp
                                        .Field("title")
                                        .Query(query)
                                    ),
                                    sh => sh.MatchPhrasePrefix(mpp => mpp
                                        .Field("displayName")
                                        .Query(query)
                                    )
                                )
                                .MinimumShouldMatch(1)
                            );
                        }
                    });

                    var filterList = new List<Action<QueryDescriptor<T>>>();

                    if (filters != null && filters.Count > 0)
                    {
                        foreach (var filter in filters)
                        {
                            filterList.Add(f => f.Term(t => t.Field(filter.Key).Value(filter.Value)));
                        }
                    }

                    if (minPrice.HasValue || maxPrice.HasValue)
                    {
                        // Use CurrentPrice for Auctions or AuctionCurrentPrice for Items
                        var priceField = indices.Contains(_settings.AuctionsIndex) ? "currentPrice" : "auctionCurrentPrice";
                        filterList.Add(f => f.Range(r => r
                            .NumberRange(nr => {
                                nr.Field(priceField);
                                if (minPrice.HasValue) nr.Gte((double)minPrice.Value);
                                if (maxPrice.HasValue) nr.Lte((double)maxPrice.Value);
                            })
                        ));
                    }

                    if (filterList.Count > 0)
                    {
                        b.Filter(filterList.ToArray());
                    }
                })
             );

            if (!string.IsNullOrEmpty(sortBy))
            {
                s.Sort(srt => srt.Field(sortBy, f => f.Order(sortDescending ? SortOrder.Desc : SortOrder.Asc)));
            }

            s.Aggregations(agg => agg
                .Add("categories", a => a.Terms(t => t.Field("categoryName.keyword")))
                .Add("statuses", a => a.Terms(t => t.Field("status.keyword")))
                .Add("auctionTypes", a => a.Terms(t => t.Field("auctionType.keyword")))
                .Add("conditions", a => a.Terms(t => t.Field("condition.keyword")))
            );
        }, cancellationToken);

        if (!response.IsSuccess())
        {
            return new SearchResponseDto<T> { Total = 0, Page = page, PageSize = pageSize };
        }

        var facets = new List<FacetDto>();
        if (response.Aggregations != null)
        {
            var catTerms = response.Aggregations.GetStringTerms("categories");
            if (catTerms != null)
            {
                facets.Add(new FacetDto("Categories", catTerms.Buckets.Select(b => new FacetBucketDto(b.Key.ToString()!, b.DocCount)).ToList()));
            }

            var statusTerms = response.Aggregations.GetStringTerms("statuses");
            if (statusTerms != null)
            {
                facets.Add(new FacetDto("Statuses", statusTerms.Buckets.Select(b => new FacetBucketDto(b.Key.ToString()!, b.DocCount)).ToList()));
            }

            var typeTerms = response.Aggregations.GetStringTerms("auctionTypes");
            if (typeTerms != null)
            {
                facets.Add(new FacetDto("Auction Types", typeTerms.Buckets.Select(b => new FacetBucketDto(b.Key.ToString()!, b.DocCount)).ToList()));
            }

            var conditionTerms = response.Aggregations.GetStringTerms("conditions");
            if (conditionTerms != null)
            {
                facets.Add(new FacetDto("Conditions", conditionTerms.Buckets.Select(b => new FacetBucketDto(b.Key.ToString()!, b.DocCount)).ToList()));
            }
        }

        return new SearchResponseDto<T>
        {
            Results = response.Documents.ToList(),
            Total = response.Total,
            Page = page,
            PageSize = pageSize,
            Facets = facets
        };
    }

    private string GetCacheKey(string prefix, string query, string[] indices, int? page = null, int? pageSize = null, string? sortBy = null, bool? sortDesc = null, Dictionary<string, string>? filters = null, decimal? minPrice = null, decimal? maxPrice = null)
    {
        var key = $"{prefix}:{string.Join(",", indices)}:q={query}";
        if (page.HasValue) key += $":p={page}";
        if (pageSize.HasValue) key += $":s={pageSize}";
        if (!string.IsNullOrEmpty(sortBy)) key += $":sb={sortBy}:{sortDesc}";
        if (minPrice.HasValue) key += $":min={minPrice}";
        if (maxPrice.HasValue) key += $":max={maxPrice}";

        if (filters != null)
        {
            var filterStr = string.Join(",", filters.OrderBy(f => f.Key).Select(f => $"{f.Key}={f.Value}"));
            key += $":f={filterStr}";
        }

        return key;
    }

    public async Task<List<string>> GetSuggestionsAsync(
        string query,
        string[] indices,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey("suggest", query, indices);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await ExecuteGetSuggestionsAsync(query, indices, ct),
            cancellationToken: cancellationToken);
    }

    private async Task<List<string>> ExecuteGetSuggestionsAsync(
        string query,
        string[] indices,
        CancellationToken cancellationToken)
    {
        var response = await _client.SearchAsync<BaseSearchDocument>(s => s
            .Indices(indices)
            .Suggest(su => su
                .Suggesters(s => s
                    .Add("hints", f => f
                        .Prefix(query)
                        .Completion(c => c
                            .Field(f => f.Suggest!)
                            .Size(5)
                            .SkipDuplicates(true)
                            .Fuzzy(fz => fz
                                .Fuzziness(new Fuzziness(1))
                                .MinLength(3)       // chỉ fuzzy khi gõ >= 3 ký tự
                                .PrefixLength(2)    // 2 ký tự đầu phải khớp chính xác
                            )
                        )
                    )
                )
            ), cancellationToken);

        if (!response.IsSuccess() || response.Suggest == null)
            return [];

        var suggestions = new List<string>();
        if (response.Suggest.TryGetValue("hints", out var suggestList))
        {
            foreach (var suggest in suggestList)
            {
                if (suggest is CompletionSuggest<BaseSearchDocument> completionSuggest)
                {
                    foreach (var option in completionSuggest.Options)
                    {
                        suggestions.Add(option.Text);
                    }
                }
            }
        }

        return suggestions.Distinct().ToList();
    }

    public async Task RecreateIndicesAsync(CancellationToken cancellationToken = default)
    {
        var indices = new[]
        {
            _settings.AuctionsIndex,
            _settings.ItemsIndex,
            _settings.UsersIndex,
            _settings.OrdersIndex,
            _settings.ShipmentsIndex,
            _settings.WarehouseIndex
        };

        foreach (var index in indices)
        {
            // Delete if exists (ignore error if not exists)
            await _client.Indices.DeleteAsync(index, cancellationToken);

            // Create with mapping
            await _client.Indices.CreateAsync<BaseSearchDocument>(index, c => c
                .Settings(s => s
                    .Analysis(a => a
                        .Analyzers(an => an
                            .Custom("vietnamese_autocomplete", ca => ca
                                .Tokenizer("standard")
                                .Filter(new[] { "lowercase", "asciifolding" })
                            )
                        )
                    )
                )
                .Mappings(m => m
                    .Properties(p => p
                        .Completion(f => f.Suggest!, cp => cp.Analyzer("vietnamese_autocomplete"))
                        .Text("status", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("categoryName", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("roles", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("shipmentType", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("providerCode", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("condition", t => t.Fields(f => f.Keyword("keyword", k => { })))
                        .Text("auctionType", t => t.Fields(f => f.Keyword("keyword", k => { })))
                    )
                )
            , cancellationToken);
        }
    }
}
