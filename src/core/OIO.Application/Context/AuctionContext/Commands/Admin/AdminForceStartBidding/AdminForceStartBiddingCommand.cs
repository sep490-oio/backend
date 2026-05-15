using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceStartBidding;

public sealed record AdminForceStartBiddingCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AdminForceStartBiddingCommand.Check()
            .WithOwnerName("AdminForceStartBidding")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class AdminForceStartBiddingCommandHandler
    : ICommandHandler<AdminForceStartBiddingCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OIO.Application.Abstractions.Scheduling.IAuctionScheduler _scheduler;

    public AdminForceStartBiddingCommandHandler(
        IDbContext dbContext,
        IClock clock,
        IUnitOfWork unitOfWork,
        OIO.Application.Abstractions.Scheduling.IAuctionScheduler scheduler)
    {
        _dbContext = dbContext;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _scheduler = scheduler;
    }

    public async Task<UnitResult<Error>> Handle(
        AdminForceStartBiddingCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(x => x.Deposits)
                .Include(x => x.Participants)
                .Include(x => x.BuyNowReservations)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!auction.Info.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;

        if (auction.Status != AuctionStatus.Scheduled)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "force start bidding");

        var nowUtc = _clock.UtcNow;

        // Force Start Bidding requires updating the StartTime to now
        // so that the AuctionInfo is accurate.
        var forceStartResult = auction.ForceStartBidding(nowUtc);
        if (forceStartResult.IsFailure)
            return forceStartResult.Error;

        // Persist the StartTime change before ActivationService triggers Start()
        // Wait, ForceStartBidding ALREADY calls Start() inside it and sets Status = Active!
        // So auction.Status is now Active. 
        // We still need to sync the Item status and schedule the end job.
        
        var startItem = await _dbContext.GetByIdAsync<OIO.Domain.Context.CatalogContext.Aggregates.Items.Item, OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId>(
            auction.ItemId,
            cancellationToken: cancellationToken);

        if (startItem is not null)
        {
            var startItemSyncResult = startItem.MarkInAuction(nowUtc);
            if (startItemSyncResult.IsFailure)
                return startItemSyncResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Schedule End
        await _scheduler.ScheduleEndAsync(auction.Id.Value, auction.Info.EndTime, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
