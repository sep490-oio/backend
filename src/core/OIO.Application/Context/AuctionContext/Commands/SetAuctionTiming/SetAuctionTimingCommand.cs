using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.SetAuctionTiming;

public sealed record SetAuctionTimingCommand(
    Guid AuctionId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime QualificationStartAt,
    DateTime QualificationEndAt,
    bool AutoExtend = true,
    int ExtensionMinutes = 5) : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return SetAuctionTimingCommand.Check()
            .WithOwnerName("SetAuctionTiming")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(QualificationStartAt)
            .NotInPast(() => DateTime.UtcNow)
            .Field(QualificationEndAt)
            .NotInPast(() => DateTime.UtcNow)
            .Field(ExtensionMinutes)
            .BetweenInclusive(1, 30);
    }
}

internal sealed class SetAuctionTimingCommandHandler
    : ICommandHandler<SetAuctionTimingCommand, AuctionDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;
    private readonly IAuctionScheduler _scheduler;
    private readonly AuctionActivationService _auctionActivationService;

    public SetAuctionTimingCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings,
        IClock clock,
        IAuctionScheduler scheduler,
        AuctionActivationService auctionActivationService)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
        _scheduler = scheduler;
        _auctionActivationService = auctionActivationService;
    }

    public async Task<Result<AuctionDto, Error>> Handle(
        SetAuctionTimingCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: q => q
                .Include(a => a.Item).ThenInclude(i => i.Media)
                .Include(a => a.Deposits)
                .Include(a => a.Participants),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != _currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerOfItem;

        if (auction.Status != AuctionStatus.Approved)
            return AuctionErrors.Auction.CannotSetTiming;

        if (auction.AuctionType == AuctionType.Sealed && request.AutoExtend)
        {
            return Error.Validation(
                "AutoExtend",
                "Auction.SealedAutoExtendNotSupported",
                "Sealed auctions do not support auto-extend.");
        }

        var qualificationResult = QualificationWindow.Create(
            request.QualificationStartAt,
            request.QualificationEndAt);

        if (qualificationResult.IsFailure)
            return qualificationResult.Error;

        var (_, isFailure, auctionInfo, error) = AuctionInfo.Create(
            nowUtc: nowUtc,
            startTime: request.StartTime,
            endTime: request.EndTime,
            autoExtend: request.AutoExtend,
            extensionMinutes: request.ExtensionMinutes,
            qualification: qualificationResult.Value);

        if (isFailure) return error;

        var result = auction.SetTiming(auctionInfo, nowUtc);
        if (result.IsFailure) return result.Error;

        // Sync linked Item to InAuction now that auction has entered Scheduled.
        // MarkInAuction is idempotent (no-op unless item is Approved/Active).
        if (auction.Status == AuctionStatus.Scheduled || auction.Status == AuctionStatus.Active)
        {
            var itemSyncResult = auction.Item.MarkInAuction(nowUtc);
            if (itemSyncResult.IsFailure)
                return itemSyncResult.Error;
        }

        _dbContext.Update(auction);

        // After timing is configured, schedule the auction start or activate immediately if start time is in the past.
        if (auction.Status == AuctionStatus.Scheduled && auction.Info!.HasStarted(nowUtc))
        {
            var activationResult = await _auctionActivationService.ActivateScheduledAuctionAsync(
                auction,
                nowUtc,
                cancellationToken);
            if (activationResult.IsFailure)
                return activationResult.Error;
        }
        else
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (auction.Status == AuctionStatus.Scheduled)
            {
                await _scheduler.ScheduleStartAsync(
                    auction.Id.Value,
                    auction.Info!.StartTime,
                    cancellationToken);
            }
        }

        return auction.ToDto(nowUtc,
            _runtimeSettings.Auction.ExtensionThreshold);
    }
}


