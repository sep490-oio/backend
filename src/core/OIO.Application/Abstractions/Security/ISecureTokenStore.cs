using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Abstractions.Security;

public interface ISecureTokenStore
{
    /// <summary>
    /// Store a token for a user. Replaces any existing token of the same type.
    /// Returns the plain token (not the hash).
    /// </summary>
    Task<string> CreateTokenAsync(
        TokenType type,
        UserId userId,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a plain token against the stored hash.
    /// Returns metadata if valid, null if invalid/expired/not found.
    /// </summary>
    Task<bool> ValidateTokenAsync(
        TokenType type,
        UserId userId,
        string plainToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate (delete) a token after successful use.
    /// </summary>
    Task InvalidateTokenAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an active token exists for a user (for rate limiting).
    /// </summary>
    Task<bool> HasActiveTokenAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get remaining TTL for a token (for cooldown enforcement).
    /// Returns null if no token exists.
    /// </summary>
    Task<TimeSpan?> GetTokenTtlAsync(
        TokenType type,
        UserId userId,
        CancellationToken cancellationToken = default);
}

public enum TokenType
{
    EmailVerification,
    PasswordReset,
    PhoneVerification,
    TwoFactorSetup,
    AccountDeletion
}
