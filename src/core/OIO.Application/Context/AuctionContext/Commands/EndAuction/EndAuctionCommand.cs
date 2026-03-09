using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.EndAuction;

public sealed record EndAuctionCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return EndAuctionCommand.Check()
            .WithOwnerName("EndAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class EndAuctionCommandHandler
    : ICommandHandler<EndAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EndAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        EndAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Bids)
                .Include(a => a.AutoBids)
                .Include(a => a.Watchers)
                .Include(a => a.PriceHistories),
            cancellationToken: cancellationToken
            );

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var now = _clock.UtcNow;

        // Idempotent: skip if already ended/sold/failed/cancelled
        if (auction.Status != AuctionStatus.Active)
            return UnitResult.Success<Error>();

        // Step 1: End the auction (determine winner, mark bids)
        var endResult = auction.End(now);
        
        if (endResult.IsFailure)
            return endResult;

        // Step 2: Resolve outcome (Sold vs Failed)
        var resolveResult = auction.Resolve(now);
        
        if (resolveResult.IsFailure)
            return resolveResult;

        // Step 3: Save
        // Domain events (AuctionEndedEvent + AuctionSoldEvent/AuctionFailedEvent)
        // will be dispatched via outbox
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}