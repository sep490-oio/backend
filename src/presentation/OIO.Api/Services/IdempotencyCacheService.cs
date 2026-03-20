using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Hybrid;
using OIO.Application.Abstractions.Caching;

namespace OIO.Api.Services;

public enum IdempotencyFailureKind
{
    MissingKey,
    PayloadMismatch,
    CacheCorrupted
}

public sealed class IdempotencyCacheService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Gates = new();

    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(15),
        LocalCacheExpiration = TimeSpan.FromMinutes(15)
    };

    public IdempotencyCacheService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> ExecuteAsync<TResponse>(
        string? idempotencyKey,
        string cacheKeyPrefix,
        string fingerprint,
        Func<Task<TResponse>> action,
        Func<IdempotencyFailureKind, TResponse> failureFactory,
        string[] tags,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return failureFactory(IdempotencyFailureKind.MissingKey);

        var cacheKey = $"{cacheKeyPrefix}:{idempotencyKey.Trim()}";

        var cached = await TryGetCachedAsync<TResponse>(cacheKey, fingerprint, failureFactory, cancellationToken);
        if (cached != null)
            return cached;

        var gate = Gates.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            cached = await TryGetCachedAsync<TResponse>(cacheKey, fingerprint, failureFactory, cancellationToken);
            if (cached != null)
                return cached;

            var response = await action();

            await _cache.SetAsync(
                cacheKey,
                CachedIdempotencyEnvelope.Create(fingerprint, JsonSerializer.Serialize(response)),
                _cacheOptions,
                tags,
                cancellationToken: cancellationToken);

            return response;
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<TResponse?> TryGetCachedAsync<TResponse>(
        string cacheKey,
        string fingerprint,
        Func<IdempotencyFailureKind, TResponse> failureFactory,
        CancellationToken cancellationToken)
    {
        var (exists, entry) = await _cache.TryGetValueAsync<CachedIdempotencyEnvelope>(cacheKey, cancellationToken);
        if (!exists || entry is null)
            return default;

        if (!string.Equals(entry.Fingerprint, fingerprint, StringComparison.Ordinal))
            return failureFactory(IdempotencyFailureKind.PayloadMismatch);

        try
        {
            var cached = JsonSerializer.Deserialize<TResponse>(entry.SerializedResponse);
            return cached ?? failureFactory(IdempotencyFailureKind.CacheCorrupted);
        }
        catch (JsonException)
        {
            return failureFactory(IdempotencyFailureKind.CacheCorrupted);
        }
    }

    private sealed record CachedIdempotencyEnvelope(
        string Fingerprint,
        string SerializedResponse)
    {
        public static CachedIdempotencyEnvelope Create(string fingerprint, string serializedResponse)
            => new(fingerprint, serializedResponse);
    }
}
