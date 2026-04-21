namespace OIO.Application.Abstractions.Caching;

/// <summary>
/// Targeted cache invalidation for gate-service entries (plan B5 / §3.6.4).
/// Implementations wrap <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> or
/// whatever L1+L2 store the app is wired against. Key layout is owned by the caller.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Best-effort eviction of a single cache key. Must not throw on miss.
    /// </summary>
    Task InvalidateAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort eviction of every entry tagged with <paramref name="tag"/>.
    /// Used by <see cref="OIO.Domain.Context.UserContext.Aggregates.Users.Events.TermsDocumentActivatedEvent"/>
    /// handler to drop all <c>terms:gate:*</c> entries for a given <c>TermType</c> in one pass.
    /// </summary>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
}
