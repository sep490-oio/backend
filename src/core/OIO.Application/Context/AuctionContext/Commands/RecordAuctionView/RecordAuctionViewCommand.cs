using System.Security.Cryptography;
using System.Text;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RecordAuctionView;

public sealed record RecordAuctionViewCommand(
    Guid AuctionId,
    string? BrowserViewerId = null,
    string? IpAddress = null) : ICommand;

internal sealed class RecordAuctionViewCommandHandler
    : ICommandHandler<RecordAuctionViewCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public RecordAuctionViewCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<UnitResult<Error>> Handle(
        RecordAuctionViewCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var nowUtc = _clock.UtcNow;

        // Resolve viewer identity
        UserId? userId = _currentUser.IsAuthenticated ? _currentUser.UserId : null;
        var browserViewerId = request.BrowserViewerId;
        string? ipHash = null;

        if (browserViewerId is null && userId is null)
        {
            // Anonymous without browser viewer ID — fall back to IP hash
            ipHash = request.IpAddress is not null
                ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.IpAddress)))[..16]
                : null;

            if (ipHash is null)
                return UnitResult.Success<Error>(); // No way to identify viewer
        }

        // Load auction with Item to check SellerId
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            q => q.Include(a => a.Item),
            cancellationToken);

        if (auction is null)
            return UnitResult.Success<Error>();

        // Seller self-view — don't count
        if (userId is not null && userId.Value == auction.Item.SellerId)
            return UnitResult.Success<Error>();

        // Query for existing view record
        var existing = await FindExistingRecord(
            auctionId, userId, browserViewerId, ipHash, cancellationToken);

        if (existing is not null)
        {
            existing.MarkSeen(nowUtc);

            if (userId is not null && existing.UserId is null)
                existing.MergeUserId(userId.Value);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<Error>();
        }

        // Create new record and increment view
        var record = AuctionViewRecord.Create(
            auctionId, userId, browserViewerId, ipHash, nowUtc);

        _dbContext.Insert(record);
        auction.IncrementView(nowUtc);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Race condition: unique constraint violation — another request already inserted.
            // Treat as existing record — just mark seen.
            _dbContext.DetachAll();

            var raceRecord = await FindExistingRecord(
                auctionId, userId, browserViewerId, ipHash, cancellationToken);

            if (raceRecord is not null)
            {
                raceRecord.MarkSeen(nowUtc);

                if (userId is not null && raceRecord.UserId is null)
                    raceRecord.MergeUserId(userId.Value);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        return UnitResult.Success<Error>();
    }

    private async Task<AuctionViewRecord?> FindExistingRecord(
        AuctionId auctionId,
        UserId? userId,
        string? browserViewerId,
        string? ipHash,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<AuctionViewRecord>().AsQueryable();

        if (userId is not null)
        {
            // Authenticated: match by userId OR browserViewerId
            return await query.FirstOrDefaultAsync(
                r => r.AuctionId == auctionId &&
                     (r.UserId == userId.Value || r.BrowserViewerId == browserViewerId),
                cancellationToken);
        }

        if (browserViewerId is not null)
        {
            // Anonymous with browser viewer ID
            return await query.FirstOrDefaultAsync(
                r => r.AuctionId == auctionId && r.BrowserViewerId == browserViewerId,
                cancellationToken);
        }

        // Anonymous IP-only fallback
        return await query.FirstOrDefaultAsync(
            r => r.AuctionId == auctionId &&
                 r.IpHash == ipHash &&
                 r.BrowserViewerId == null &&
                 r.UserId == null,
            cancellationToken);
    }
}
