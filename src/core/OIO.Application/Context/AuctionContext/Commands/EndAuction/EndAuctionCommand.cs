using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
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
    private readonly ICurrentUser _currentUser;
    private readonly ISealedBidEncryptionService _sealedBidEncryptionService;

    public EndAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ICurrentUser currentUser,
        ISealedBidEncryptionService sealedBidEncryptionService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _currentUser = currentUser;
        _sealedBidEncryptionService = sealedBidEncryptionService;
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
                .Include(a => a.BuyNowReservations)
                .Include(a => a.SealedBids)
                .Include(a => a.Watchers)
                .Include(a => a.PriceHistories)
                .Include(a => a.Item),
            cancellationToken: cancellationToken
            );

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (_currentUser.IsAuthenticated &&
            auction.Item.SellerId != _currentUser.UserId &&
            !_currentUser.IsInRole(App.Roles.Catalogs.Admin))
        {
            return Error.Forbidden(
                "Auction.OnlyOwnerCanClose",
                "Only the auction owner or an admin can close this auction.");
        }

        var now = _clock.UtcNow;

        // Idempotent: skip if already ended/sold/failed/cancelled
        if (auction.Status != AuctionStatus.Active)
            return UnitResult.Success<Error>();

        if (auction.GetActiveBuyNowReservation(now) is not null)
            return UnitResult.Success<Error>();

        if (auction.AuctionType == AuctionType.Sealed)
        {
            var revealedSealedBids = new List<RevealedSealedBidAmount>(auction.SealedBids.Count);

            foreach (var sealedBid in auction.SealedBids)
            {
                var decryptedAmount = _sealedBidEncryptionService.Decrypt(sealedBid.AmountEncrypted);

                if (decryptedAmount.IsFailure)
                    return decryptedAmount.Error;

                var amount = OIO.Domain.Context.Shared.ValueObjects.Money.Create(
                    decryptedAmount.Value,
                    auction.Pricing.Currency);

                if (amount.IsFailure)
                    return amount.Error;

                revealedSealedBids.Add(new RevealedSealedBidAmount(sealedBid.Id, amount.Value));
            }

            var revealResult = auction.RevealAllSealedBids(
                revealedSealedBids,
                _currentUser.IsAuthenticated ? _currentUser.UserId : null,
                now);

            if (revealResult.IsFailure)
                return revealResult.Error;
        }

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
