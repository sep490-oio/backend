using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Caching;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Services;

internal sealed class SessionRevocationStore : ISessionRevocationStore
{
    private const string Prefix = "revoked";
    private readonly HybridCache _cache;
    private readonly HybridCacheEntryOptions _cacheOptions;
    
    public SessionRevocationStore(
        HybridCache cache,
        IOptionsMonitor<JwtOptions> jwtOptions)
    {
        _cache = cache;
        _cacheOptions = new HybridCacheEntryOptions()
        {
            Expiration = jwtOptions.CurrentValue.AccessTokenExpiration,
            LocalCacheExpiration = jwtOptions.CurrentValue.AccessTokenExpiration
        };
    }

    public async Task RevokeDeviceAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var key = ForDevice(userId.Value, deviceId);
        await _cache.SetAsync(key, true, _cacheOptions, [$"revoked:{userId}:device"], cancellationToken: cancellationToken);
    }

    public async Task<bool> IsDeviceRevokedAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var key = ForDevice(userId.Value, deviceId);
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
        var key = ForUser(userId.Value);
        await _cache.SetAsync(key, true, _cacheOptions, [$"revoked:{userId}:all-device"], cancellationToken: cancellationToken);
    }

    public async Task<bool> IsUserRevokedAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = ForUser(userId.Value);
        return await _cache.ExistsAsync(key, cancellationToken);
    }

    public async Task ClearUserRevocationAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync([$"revoked:{userId}:all-device"], cancellationToken);
    }
    
    

    /// <summary>
    /// Key for device-level revocation.
    /// Pattern: "revoked:device:{userId}:{deviceId}"
    /// </summary>
    private static string ForDevice(Guid userId, Guid deviceId)
        => $"{Prefix}:device:{userId}:{deviceId}";
    

    /// <summary>
    /// Key for user-level (all devices) revocation.
    /// Pattern: "revoked:user:{userId}"
    /// </summary>
    private static string ForUser(Guid userId)
        => $"{Prefix}:user:{userId}";
}