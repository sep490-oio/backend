using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Services;

internal sealed class AuctionActivationService
{
    internal const string NoEligibleParticipantsAutoCancelReason =
        "Auction automatically cancelled because no bid-eligible participants were present at the scheduled start time.";

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuctionScheduler _scheduler;

    public AuctionActivationService(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IAuctionScheduler scheduler)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _scheduler = scheduler;
    }

    public async Task<UnitResult<Error>> ActivateScheduledAuctionAsync(
        Auction auction,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (auction.Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!auction.Info.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;

        if (auction.Status != AuctionStatus.Scheduled)
            return UnitResult.Success<Error>();

        if (!auction.HasBidEligibleParticipants(nowUtc))
        {
            var cancelResult = auction.CancelAuction(NoEligibleParticipantsAutoCancelReason, nowUtc);
            if (cancelResult.IsFailure)
                return cancelResult.Error;

            var item = await _dbContext.GetByIdAsync<Item, ItemId>(
                auction.ItemId,
                cancellationToken: cancellationToken);

            if (item is not null)
            {
                var returnItemResult = item.ReturnToActive(nowUtc);
                if (returnItemResult.IsFailure)
                    return returnItemResult.Error;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _scheduler.CancelAsync(auction.Id.Value, cancellationToken);
            return UnitResult.Success<Error>();
        }

        var startResult = auction.Start(nowUtc);
        if (startResult.IsFailure)
            return startResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _scheduler.ScheduleEndAsync(auction.Id.Value, auction.Info.EndTime, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
