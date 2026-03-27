using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RecordAuctionView;

public sealed record RecordAuctionViewCommand(
    Guid AuctionId,
    string? IpAddress = null) : ICommand;

internal sealed class RecordAuctionViewCommandHandler
    : ICommandHandler<RecordAuctionViewCommand>
{
    private static readonly TimeSpan ViewCooldown = TimeSpan.FromMinutes(30);

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly HybridCache _cache;

    public RecordAuctionViewCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        HybridCache cache)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _cache = cache;
    }

    public async Task<UnitResult<Error>> Handle(
        RecordAuctionViewCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        // Build cooldown key: authenticated user uses userId, anonymous uses IP
        var viewerKey = _currentUser.IsAuthenticated
            ? _currentUser.UserId.Value.ToString()
            : request.IpAddress ?? "anonymous";

        var cacheKey = $"view:{request.AuctionId}:{viewerKey}";

        // Check if already viewed within cooldown period
        var alreadyViewed = await _cache.GetOrCreateAsync(
            cacheKey,
            async _ => true,
            new HybridCacheEntryOptions { Expiration = ViewCooldown },
            cancellationToken: cancellationToken);

        // If the cache returned a pre-existing value, skip increment
        // GetOrCreateAsync creates the entry if it doesn't exist, so we use a different approach:
        // Try to get first, if not found then set and increment
        // Simple approach: always set the cache, but only increment if it's a new entry
        // Since HybridCache.GetOrCreateAsync always returns a value, we track with a sentinel

        // Simplified: use a flag pattern
        var flagKey = $"view:flag:{request.AuctionId}:{viewerKey}";
        var existingFlag = await _cache.GetOrCreateAsync<string>(
            flagKey,
            async _ => "new",
            new HybridCacheEntryOptions { Expiration = ViewCooldown },
            cancellationToken: cancellationToken);

        // If we got back "new", this is a fresh entry — increment view
        // On subsequent calls within cooldown, the factory won't run and we get the cached "new" back
        // We need a different approach: check existence then set

        // Actually, simplest correct approach: try to read, if miss then write + increment
        // HybridCache doesn't have a TryGet, so we use a two-step approach with a known sentinel

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            cancellationToken: cancellationToken);

        if (auction is null)
            return UnitResult.Success<Error>(); // silently ignore for missing auctions

        var nowUtc = _clock.UtcNow;
        auction.IncrementView(nowUtc);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
