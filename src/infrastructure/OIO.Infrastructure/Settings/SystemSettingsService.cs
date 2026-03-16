using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Settings;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using StackExchange.Redis;

namespace OIO.Infrastructure.Settings;

internal sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HybridCache _cache;
    private readonly ILogger<SystemSettingsService> _logger;
    private readonly IClock _clock;

    private const string CachePrefix = "settings:";
    private const string CacheTag = "system_settings";

    public SystemSettingsService(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        HybridCache cache,
        ILogger<SystemSettingsService> logger,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
        _clock = clock;
    }

    public async Task<T> GetAsync<T>(
        string key, 
        T defaultValue, 
        CancellationToken ct = default)
    {
        // 1. Check Redis cache
        var cacheKey = $"{CachePrefix}{key}";
        
        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var systemSettingId = SystemSettingId.From(key);
                // 2. Check DB
                var setting = await _dbContext.Set<SystemSetting>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == systemSettingId, cancel);

                if (setting is null)
                    return defaultValue;

                var value = JsonSerializer.Deserialize<T>(setting.Value)!;
                
                return value;
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(30),
                LocalCacheExpiration = TimeSpan.FromMinutes(5)
            },
            tags: [CacheTag, cacheKey],
            cancellationToken: ct);

        return result;
    }

    public async Task SetAsync<T>(
        string key, 
        T value,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value);

        var systemSettingId = SystemSettingId.From(key);
        var setting = await _dbContext.Set<SystemSetting>()
            .FirstOrDefaultAsync(s => s.Id == systemSettingId, ct);
        
        var nowUtc = _clock.UtcNow;
        
        if (setting is null)
        {
            setting = SystemSetting.Create(
                nowUtc,
                key,
                json,
                typeof(T).Name);
            _dbContext.Set<SystemSetting>().Add(setting);
        }
        else
        {
            setting.Update(nowUtc, json);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Invalidate cache
        var cacheKey = $"{CachePrefix}{key}";
        await _cache.RemoveByTagAsync(cacheKey, ct);

        _logger.LogInformation("Setting updated: {Key}", key);
    }

    public async Task InvalidateCacheAsync(
        string? key = null, 
        CancellationToken ct = default)
    {
        var tag = key is not null ? $"{CachePrefix}{key}" : "system-settings";
        await _cache.RemoveByTagAsync(tag, ct);
        _logger.LogInformation("Settings cache invalidated. Key={Key}", key ?? "ALL");
    }
}