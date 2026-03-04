namespace OIO.Application.Abstractions.Caching.CacheKeys;

public static class RevocationCacheKeys
{
    private const string Prefix = "revoked";

    /// <summary>
    /// Key for device-level revocation.
    /// Pattern: "revoked:device:{userId}:{deviceId}"
    /// </summary>
    public static string ForDevice(Guid userId, Guid deviceId)
        => $"{Prefix}:device:{userId}:{deviceId}";
    

    /// <summary>
    /// Key for user-level (all devices) revocation.
    /// Pattern: "revoked:user:{userId}"
    /// </summary>
    public static string ForUser(Guid userId)
        => $"{Prefix}:user:{userId}";
}