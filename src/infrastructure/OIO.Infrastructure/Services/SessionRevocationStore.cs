using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Caching;
using OIO.Application.Abstractions.Caching.CacheKeys;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Services;

internal sealed class SessionRevocationStore : ISessionRevocationStore
{
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _cacheOptions;
    
    public SessionRevocationStore(
        HybridCache cache,
        IOptions<JwtOptions> jwtOptions)
    {
        _cache = cache;
        _cacheOptions = new HybridCacheEntryOptions()
        {
            Expiration = jwtOptions.Value.AccessTokenExpiration,
            LocalCacheExpiration = jwtOptions.Value.AccessTokenExpiration
        };
    }

    public async Task RevokeDeviceAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var key = RevocationCacheKeys.ForDevice(userId.Value, deviceId);
        await _cache.SetAsync(key, true, _cacheOptions, [$"revoked:{userId}:device"], cancellationToken: cancellationToken);
    }

    public async Task<bool> IsDeviceRevokedAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var key = RevocationCacheKeys.ForDevice(userId.Value, deviceId);
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    public async Task ClearDeviceRevocationAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync([$"revoked:{userId}:device"], cancellationToken);
    }
    
    public async Task RevokeAllDevicesAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = RevocationCacheKeys.ForUser(userId.Value);
        await _cache.SetAsync(key, true, _cacheOptions, [$"revoked:{userId}:all-device"], cancellationToken: cancellationToken);
    }

    public async Task<bool> IsUserRevokedAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = RevocationCacheKeys.ForUser(userId.Value);
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    public async Task ClearUserRevocationAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync([$"revoked:{userId}:all-device"], cancellationToken);
    }
}