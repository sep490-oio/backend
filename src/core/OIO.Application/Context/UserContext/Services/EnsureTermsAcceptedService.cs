using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Services;

/// <summary>
/// Implements <see cref="IEnsureTermsAcceptedService"/> with HybridCache L1+L2 caching
/// (plan §3.6.4 / B6). Cache key is <c>terms:gate:{userId}:{fingerprint}</c> where fingerprint
/// is an ordered concatenation of active doc IDs per requested term type. On activation,
/// <c>TermsDocumentActivatedEventHandler</c> invalidates by tag <c>terms:{TermType}</c>.
///
/// Redis-down degradation: cache exceptions are logged as <c>terms_gate_cache_degraded_total</c>
/// and the request falls through to a direct DB read (fail-closed to correctness).
/// </summary>
internal sealed class EnsureTermsAcceptedService : IEnsureTermsAcceptedService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IDbContext _dbContext;
    private readonly HybridCache _cache;
    private readonly ILogger<EnsureTermsAcceptedService> _logger;

    public EnsureTermsAcceptedService(
        IDbContext dbContext,
        HybridCache cache,
        ILogger<EnsureTermsAcceptedService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UnitResult<Error>> EnsureAsync(
        UserId userId,
        IEnumerable<string> requiredTermTypes,
        CancellationToken cancellationToken = default)
    {
        var normalizedTypes = requiredTermTypes
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLowerInvariant())
            .Distinct()
            .OrderBy(t => t, StringComparer.Ordinal)
            .ToList();
        if (normalizedTypes.Count == 0)
            return UnitResult.Success<Error>();

        // Fingerprint from the active docs for the requested types. This bounds cache lifetime
        // to the "current active set" — when an activation flips the active doc for a type, the
        // fingerprint changes and the cache tag invalidation in the handler evicts the old entry.
        var activeDocs = await _dbContext.Set<TermsDocument>()
            .AsNoTracking()
            .Where(x => x.Status == TermsDocumentStatus.Active
                        && normalizedTypes.Contains(x.TermType.ToLower()))
            .Select(x => new { x.Id, TermType = x.TermType.ToLower() })
            .ToListAsync(cancellationToken);

        var activeDocIdsByType = activeDocs
            .GroupBy(x => x.TermType)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Id.Value).OrderBy(id => id).ToList());

        var fingerprint = string.Join(
            "|",
            normalizedTypes.Select(t =>
                activeDocIdsByType.TryGetValue(t, out var ids)
                    ? $"{t}={string.Join(",", ids.Select(id => id.ToString("N", CultureInfo.InvariantCulture)))}"
                    : $"{t}=none"));

        var cacheKey = $"terms:gate:{userId.Value:N}:{Sha(fingerprint)}";
        var tags = normalizedTypes
            .Select(t => $"terms:{t}")
            .Append($"terms:gate:user:{userId.Value:N}")
            .ToArray();

        // Cache layer: try L1+L2 HybridCache. On any exception, fall through to direct DB — fail-closed.
        try
        {
            var cached = await _cache.GetOrCreateAsync<CacheEntry>(
                cacheKey,
                async ct =>
                {
                    var pending = await ComputePendingTermsAsync(userId, normalizedTypes, activeDocIdsByType, ct);
                    return pending is null
                        ? new CacheEntry(IsPending: false, PendingType: null)
                        : new CacheEntry(IsPending: true, PendingType: pending);
                },
                options: new HybridCacheEntryOptions { Expiration = CacheTtl, LocalCacheExpiration = CacheTtl },
                tags: tags,
                cancellationToken: cancellationToken);

            return cached.IsPending
                ? Error.Conflict(
                    "Terms.PendingAcceptance",
                    $"Please accept the updated {cached.PendingType} terms to continue.")
                : UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            // Redis-down / cache misbehavior: log degradation metric and fall through to direct DB.
            _logger.LogWarning(ex,
                "metric=terms_gate_cache_degraded_total UserId={UserId}: falling back to direct DB read.",
                userId.Value);

            var pending = await ComputePendingTermsAsync(userId, normalizedTypes, activeDocIdsByType, cancellationToken);
            return pending is null
                ? UnitResult.Success<Error>()
                : Error.Conflict(
                    "Terms.PendingAcceptance",
                    $"Please accept the updated {pending} terms to continue.");
        }
    }

    /// <summary>
    /// Returns the first required term type for which the user has NOT accepted the current
    /// active document, or null if all required terms are accepted (or have no active doc).
    /// </summary>
    private async Task<string?> ComputePendingTermsAsync(
        UserId userId,
        IReadOnlyList<string> normalizedTypes,
        IReadOnlyDictionary<string, List<Guid>> activeDocIdsByType,
        CancellationToken cancellationToken)
    {
        // Collect active doc ids across all requested types in one pass.
        var activeDocIds = activeDocIdsByType
            .SelectMany(kvp => kvp.Value)
            .Select(TermsDocumentId.From)
            .ToList();

        if (activeDocIds.Count == 0)
        {
            // No active doc for any requested type ⇒ no enforceable terms. Treat as accepted.
            return null;
        }

        var acceptedIds = await _dbContext.Set<TermsAcceptance>()
            .AsNoTracking()
            .Where(a => a.UserId == userId && activeDocIds.Contains(a.TermDocumentId))
            .Select(a => a.TermDocumentId.Value)
            .ToListAsync(cancellationToken);

        var acceptedSet = acceptedIds.ToHashSet();

        foreach (var type in normalizedTypes)
        {
            if (!activeDocIdsByType.TryGetValue(type, out var idsForType))
                continue; // no active doc for this type ⇒ nothing to enforce

            // User must have accepted AT LEAST ONE of the active ids for this type. In practice
            // there is exactly one Active doc per type (domain invariant + command-handler
            // superseding), but we guard against multi-active anomalies by accepting any.
            if (!idsForType.Any(acceptedSet.Contains))
                return type;
        }

        return null;
    }

    private static string Sha(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        // 16 hex chars is enough disambiguation for a per-user scoped key.
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }

    /// <summary>
    /// Serializable cache entry. A bool+string pair keeps the payload tiny even for L2 Redis.
    /// </summary>
    internal sealed record CacheEntry(bool IsPending, string? PendingType);
}
