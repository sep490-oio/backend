using Microsoft.Extensions.Caching.Hybrid;

namespace OIO.Application.Abstractions.Caching;

public static class HybridCacheExtensions
{
    private static readonly HybridCacheEntryOptions Options = new HybridCacheEntryOptions()
    {
        Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite
    };

    /// <summary>
    /// Returns true if the cache contains an item with a matching key.
    /// </summary>
    /// <param name="cache">An instance of <see cref="HybridCache"/></param>
    /// <param name="key">The name (key) of the item to search for in the cache.</param>
    /// <param name="cancellation">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the item exists already. False if it doesn't.</returns>
    /// <remarks>Will never add or alter the state of any items in the cache.</remarks>
    public async static Task<bool> ExistsAsync(this HybridCache cache, string key, CancellationToken cancellation = default)
    {
        (var exists, _) = await TryGetValueAsync<object>(cache, key, cancellation);
        return exists;
    }

    /// <summary>
    /// Returns true if the cache contains an item with a matching key, along with the value of the matching cache entry.
    /// </summary>
    /// <typeparam name="T">The type of the value of the item in the cache.</typeparam>
    /// <param name="cache">An instance of <see cref="HybridCache"/></param>
    /// <param name="key">The name (key) of the item to search for in the cache.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A tuple of <see cref="bool"/> and the object (if found) retrieved from the cache.</returns>
    /// <remarks>Will never add or alter the state of any items in the cache.</remarks>
    public async static Task<(bool Exists, T? Value)> TryGetValueAsync<T>(this HybridCache cache, string key, CancellationToken cancellationToken = default)
    {

        var result = await cache.GetOrCreateAsync<object, T?>(
            key,
            null!,
            (_, _) => ValueTask.FromResult<T?>(default),
            Options,
            null,
            CancellationToken.None);

        if (result == null!)
        {
            return (false, default);
        }
        
        return (true, result);
    }
}