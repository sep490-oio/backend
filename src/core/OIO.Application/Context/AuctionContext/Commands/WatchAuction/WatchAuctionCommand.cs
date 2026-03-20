using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.WatchAuction;

public sealed record WatchAuctionCommand(
    Guid AuctionId,
    bool NotifyOnBid = true,
    bool NotifyOnEnd = true) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return WatchAuctionCommand.Check()
            .WithOwnerName("WatchAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class WatchAuctionCommandHandler
    : ICommandHandler<WatchAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;
    
    public WatchAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        WatchAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Watchers).Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = _clock.UtcNow;
        
        var result = auction.AddWatcher(
                _currentUser.UserId,
                nowUtc,
                request.NotifyOnBid,
                request.NotifyOnEnd);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result;
    }
}