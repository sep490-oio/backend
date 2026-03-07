using System.Text.Json;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Security;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using StackExchange.Redis;

namespace OIO.Infrastructure.Security;

internal sealed class SecureTokenStore : ISecureTokenStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;
    private readonly ILogger<SecureTokenStore> _logger;

    private IDatabase Db => _redis.GetDatabase();

    public SecureTokenStore(
        IConnectionMultiplexer redis,
        ISecureTokenGenerator tokenGenerator,
        IAppConfigs appConfigs,
        ILogger<SecureTokenStore> logger,
        IClock clock)
    {
        _redis = redis;
        _tokenGenerator = tokenGenerator;
        _appConfigs = appConfigs;
        _clock = clock;
        _logger = logger;
    }

    public async Task<string> CreateTokenAsync(
        TokenType type,
        UserId userId,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(type, userId);
        var ttl = expiration ?? await GetDefaultExpirationAsync(type);

        // Generate plain token + hash
        var plainToken = _tokenGenerator.GenerateToken();
        var tokenHash = _tokenGenerator.HashToken(plainToken);

        // Build stored value
        var storedValue = new StoredToken
        {
            TokenHash = tokenHash,
            CreatedAt = _clock.UtcNow,
        };

        var json = JsonSerializer.Serialize(storedValue);

        // Store with TTL (replaces any existing token of same type for this user)
        await Db.StringSetAsync(key, json, ttl);

        _logger.LogInformation(
            "Created {Type} token for user {UserId}. Expires in {Expiration}.",
            type, userId, expiration);

        return plainToken;
    }

    public async Task<bool> ValidateTokenAsync(
        TokenType type,
        UserId userId,
        string plainToken,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(type, userId);

        var json = await Db.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            _logger.LogWarning(
                "Token validation failed for {Type}/{UserId}: not found or expired.",
                type, userId);
            return false;
        }

        var stored = JsonSerializer.Deserialize<StoredToken>(json.ToString());
        
        if (stored is null) 
            return false;

        // Compare hashes
        var providedHash = _tokenGenerator.HashToken(plainToken);

        if (stored.TokenHash != providedHash)
        {
            _logger.LogWarning(
                "Token validation failed for {Type}/{UserId}: hash mismatch.",
                type, userId);
            return false;
        }

        return true;
    }

    public async Task InvalidateTokenAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(type, userId);
        var deleted = await Db.KeyDeleteAsync(key);

        _logger.LogInformation(
            "Invalidated {Type} token for user {UserId}. Found={Found}.",
            type, userId, deleted);
    }

    public async Task<bool> HasActiveTokenAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(type, userId);
        return await Db.KeyExistsAsync(key);
    }

    public async Task<TimeSpan?> GetTokenTtlAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(type, userId);
        var ttl = await Db.KeyTimeToLiveAsync(key);
        return ttl;
    }

    private static string BuildKey(TokenType type, UserId userId) =>
        $"token:{type.ToString().ToLowerInvariant()}:{userId}";

    private async Task<TimeSpan> GetDefaultExpirationAsync(TokenType type, CancellationToken cancellationToken = default) => type switch
    {
        TokenType.EmailVerification => await _appConfigs.Auth.GetEmailVerificationTokenExpirationMinutesAsync(cancellationToken),
        TokenType.PasswordReset => await _appConfigs.Auth.GetPasswordResetTokenExpirationMinutesAsync(cancellationToken),
        TokenType.PhoneVerification => await _appConfigs.Auth.GetPhoneVerificationTokenExpirationMinutesAsync(cancellationToken),
        TokenType.TwoFactorSetup => await _appConfigs.Auth.GetTwoFactorSetupTokenExpirationMinutesAsync(cancellationToken),
        _ => TimeSpan.FromMinutes(30)
    };
    
    private sealed class StoredToken
    {
        public string TokenHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}