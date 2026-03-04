using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Hybrid;
using OIO.Application.Abstractions.Caching;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Services;

internal sealed class EmailConfirmationService : IEmailConfirmationService
{
    private const int TokenExpirationMinutes = 60;
    private const string CachePrefix = "email_confirmation:";

    private readonly HybridCache _cache;

    public EmailConfirmationService(HybridCache cache)
    {
        _cache = cache;
    }

    public async Task<string> GenerateTokenAsync(
        UserId userId, CancellationToken ct = default)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var cacheKey = $"{CachePrefix}{userId.ToString()}";
        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(TokenExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(TokenExpirationMinutes)
        };
        await _cache.SetAsync(
            cacheKey,
            token,
            options,
            cancellationToken: ct);

        return token;
    }

    public async Task<bool> ValidateTokenAsync(
        UserId userId, string token, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{userId.ToString()}";
        var storedToken = await _cache.TryGetValueAsync<string>(cacheKey, ct);

        if (!storedToken.Exists || !string.Equals(storedToken.Value, token, StringComparison.Ordinal))
            return false;

        // Remove token after successful validation (one-time use)
        await _cache.RemoveAsync(cacheKey, ct);
        return true;
    }
}