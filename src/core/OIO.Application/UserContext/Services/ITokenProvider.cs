using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.UserContext.Services;

public interface ITokenProvider
{
    string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName,
        Guid deviceId,
        IReadOnlyCollection<string> roles,
        DateTime now);
    
    string Generate();
}

public interface ISessionRevocationStore
{
    /// <summary>
    /// Revoke all access tokens for a specific device.
    /// Cache key: "revoked:device:{userId}:{deviceId}"
    /// </summary>
    Task RevokeDeviceAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a specific device's tokens are revoked.
    /// </summary>
    Task<bool> IsDeviceRevokedAsync(
        UserId userId,
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove device revocation (e.g., user logs in again on same device).
    /// </summary>
    Task ClearDeviceRevocationAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all access tokens for ALL devices of a user.
    /// Cache key: "revoked:user:{userId}"
    /// </summary>
    Task RevokeAllDevicesAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if all of a user's tokens are revoked.
    /// </summary>
    Task<bool> IsUserRevokedAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove user-level revocation (e.g., user logs in again).
    /// </summary>
    Task ClearUserRevocationAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}