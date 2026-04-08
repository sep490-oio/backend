using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.SubmitAuction;

public sealed record SubmitAuctionCommand(
    Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return SubmitAuctionCommand.Check()
            .WithOwnerName("SubmitAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class SubmitAuctionCommandHandler
    : ICommandHandler<SubmitAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SubmitAuctionCommandHandler(
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
        SubmitAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: q => q
                .Include(a => a.Item)
                .ThenInclude(i => i.Media),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != _currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerOfItem;

        if (auction.Status != AuctionStatus.Draft)
            return AuctionErrors.Auction.CannotSubmit;

        if (auction.Item.Status != ItemStatus.Approved && auction.Item.Status != ItemStatus.Active)
            return AuctionErrors.Item.InvalidState(auction.Item.Status.Id, "submit auction");

        var auctionResult = auction.SubmitConfiguration(nowUtc);
        if (auctionResult.IsFailure)
            return auctionResult.Error;

        // Sync linked Item to InAuction when the auction has been published into a
        // schedulable/live lifecycle state. MarkInAuction is idempotent and only
        // transitions from Approved/Active, so it is safe to call unconditionally.
        if (auction.Status == AuctionStatus.Scheduled || auction.Status == AuctionStatus.Active)
        {
            var itemSyncResult = auction.Item.MarkInAuction(nowUtc);
            if (itemSyncResult.IsFailure)
                return itemSyncResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
