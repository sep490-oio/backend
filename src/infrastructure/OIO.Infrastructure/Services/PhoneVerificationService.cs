using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Caching;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Services;

internal sealed class PhoneVerificationService : IPhoneVerificationService
{
    private const int CodeExpirationMinutes = 5;
    private const int CodeLength = 6;
    private const string CachePrefix = "phone_verification:";

    private readonly HybridCache _cache;
    private readonly ILogger<PhoneVerificationService> _logger;

    public PhoneVerificationService(
        HybridCache cache,
        ILogger<PhoneVerificationService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SendCodeAsync(
        UserId userId, PhoneNumber phoneNumber, CancellationToken ct = default)
    {
        var code = GenerateCode();
        var cacheKey = $"{CachePrefix}{userId.ToString()}:{phoneNumber.Value}";

        var options = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(CodeExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(CodeExpirationMinutes)
        };
        await _cache.SetAsync(
            cacheKey,
            code,
            options,
            cancellationToken: ct);

        // TODO: Send SMS via external provider (Twilio, etc.)
        _logger.LogInformation(
            "Phone verification code sent to {PhoneNumber} for user {UserId}",
            phoneNumber, userId);
    }

    public async Task<bool> VerifyCodeAsync(
        UserId userId, PhoneNumber phoneNumber, string code, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{userId}:{phoneNumber.Value}";
        var storedCode = await _cache.TryGetValueAsync<string>(cacheKey, ct);

        if (!storedCode.Exists || !string.Equals(storedCode.Value, code, StringComparison.Ordinal))
            return false;

        await _cache.RemoveAsync(cacheKey, ct);
        return true;
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(0, (int)Math.Pow(10, CodeLength))
            .ToString()
            .PadLeft(CodeLength, '0');
    }
}