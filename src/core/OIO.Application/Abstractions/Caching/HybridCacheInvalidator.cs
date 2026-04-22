using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;

namespace OIO.Application.Abstractions.Caching;

/// <summary>
/// <see cref="ICacheInvalidator"/> backed by <see cref="HybridCache"/> (L1 in-process + L2 Redis).
/// Evictions are best-effort: swallow and log to avoid blocking the activation transaction on
/// a transient cache outage (plan B6 degradation policy is enforced by the *read* path, not here).
/// </summary>
internal sealed class HybridCacheInvalidator : ICacheInvalidator
{
    private readonly HybridCache _cache;
    private readonly ILogger<HybridCacheInvalidator> _logger;

    public HybridCacheInvalidator(HybridCache cache, ILogger<HybridCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "metric=terms_gate_cache_invalidation_failed_total Key={Key}: cache eviction failed; next read will rebuild from DB.",
                key);
        }
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveByTagAsync(tag, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "metric=terms_gate_cache_invalidation_failed_total Tag={Tag}: tag eviction failed; next read will rebuild from DB.",
                tag);
        }
    }
}
