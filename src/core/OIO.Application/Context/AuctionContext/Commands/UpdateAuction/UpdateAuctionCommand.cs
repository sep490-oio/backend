using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.UpdateAuction;

public sealed record UpdateAuctionCommand(
    Guid AuctionId,
    decimal? StartingPrice = null,
    decimal? BidIncrement = null,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    string? Currency = null,
    string? AuctionType = null,
    DateTime? StartTime = null,
    DateTime? EndTime = null,
    DateTime? QualificationStartAt = null,
    DateTime? QualificationEndAt = null,
    bool? AutoExtend = null,
    int? ExtensionMinutes = null) : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateAuctionCommand.Check()
            .WithOwnerName("UpdateAuction")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(StartingPrice)
            .WhenHasValue(x => x.NonNegative())
            .Field(BidIncrement)
            .WhenHasValue(x => x.Positive())
            .Field(ReservePrice)
            .WhenHasValue(x => x.NonNegative())
            .Field(BuyNowPrice)
            .WhenHasValue(x => x.Positive())
            .Field(Currency)
            .WhenHasValue(x => x.ExactLength(3))
            .Field(AuctionType)
            .WhenHasValue(x => x.InSet(OIO.Domain.Context.AuctionContext.Enums.AuctionType.All.Select(a => a.Id)))
            .Field(ExtensionMinutes)
            .WhenHasValue(x => x.BetweenInclusive(1, 30));
    }
}

internal sealed class UpdateAuctionCommandHandler
    : ICommandHandler<UpdateAuctionCommand, AuctionDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuctionScheduler _scheduler;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;

    public UpdateAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAuctionScheduler scheduler,
        IRuntimeSettings runtimeSettings,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _scheduler = scheduler;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
    }

    public async Task<Result<AuctionDto, Error>> Handle(
        UpdateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = _clock.UtcNow;
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Item)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != _currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerOfItem;

        var auctionTypeId = request.AuctionType ?? auction.AuctionType?.Id ?? OIO.Domain.Context.AuctionContext.Enums.AuctionType.Regular.Id;
        var auctionType = OIO.Domain.Context.AuctionContext.Enums.AuctionType.FromId(auctionTypeId);
        if (auctionType.HasNoValue)
            return AuctionErrors.Auction.InvalidAuctionType;

        var currencyId = request.Currency ?? auction.Pricing.Currency.Id;
        var currency = Currency.FromId(currencyId);
        if (currency.HasNoValue)
            return Currency.Errors.NotSupported;

        var requestedAutoExtend = request.AutoExtend ?? auction.Info?.AutoExtend ?? false;
        if (auctionType.Value == OIO.Domain.Context.AuctionContext.Enums.AuctionType.Sealed &&
            requestedAutoExtend)
        {
            return Error.Validation(
                "AutoExtend",
                "Auction.SealedAutoExtendNotSupported",
                "Sealed auctions do not support auto-extend.");
        }

        var pricingResult = AuctionPricing.Create(
            startingPrice: request.StartingPrice ?? auction.Pricing.StartingAmount,
            bidIncrement: request.BidIncrement ?? auction.Pricing.BidIncrementAmount,
            currency: currency.Value,
            reservePrice: request.ReservePrice ?? auction.Pricing.ReserveAmount,
            buyNowPrice: request.BuyNowPrice ?? auction.Pricing.BuyNowAmount);

        if (pricingResult.IsFailure)
            return pricingResult.Error;

        var infoResult = await BuildAuctionInfoAsync(auction, request, nowUtc, cancellationToken);
        if (infoResult.IsFailure)
            return infoResult.Error;

        var updateResult = auction.UpdateConfiguration(
            auctionType.Value,
            pricingResult.Value,
            infoResult.Value,
            nowUtc);

        if (updateResult.IsFailure)
            return updateResult.Error;

        // Sync linked Item to InAuction when UpdateConfiguration promoted the auction
        // to Scheduled (Approved + Info -> Scheduled). MarkInAuction is idempotent.
        if (auction.Status == OIO.Domain.Context.AuctionContext.Enums.AuctionStatus.Scheduled ||
            auction.Status == OIO.Domain.Context.AuctionContext.Enums.AuctionStatus.Active)
        {
            var itemSyncResult = auction.Item.MarkInAuction(nowUtc);
            if (itemSyncResult.IsFailure)
                return itemSyncResult.Error;
        }

        _dbContext.Update(auction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (auction.Status == OIO.Domain.Context.AuctionContext.Enums.AuctionStatus.Scheduled && auction.Info is not null)
        {
            await _scheduler.ScheduleStartAsync(auction.Id.Value, auction.Info.StartTime, cancellationToken);
        }

        return auction.ToDto(
            nowUtc,
            _runtimeSettings.Auction.ExtensionThreshold);
    }

    private static async Task<Result<AuctionInfo?, Error>> BuildAuctionInfoAsync(
        Auction auction,
        UpdateAuctionCommand request,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var shouldRebuildInfo =
            auction.Info is not null ||
            request.StartTime.HasValue ||
            request.EndTime.HasValue ||
            request.QualificationStartAt.HasValue ||
            request.QualificationEndAt.HasValue ||
            request.AutoExtend.HasValue ||
            request.ExtensionMinutes.HasValue;

        if (!shouldRebuildInfo)
            return (AuctionInfo?)null;

        if ((request.StartTime.HasValue && !request.EndTime.HasValue) ||
            (!request.StartTime.HasValue && request.EndTime.HasValue))
        {
            return Error.Validation(
                "Timing",
                "Auction.InvalidPeriod",
                "Start time and end time must be provided together.");
        }

        var startTime = request.StartTime ?? auction.Info?.StartTime;
        var endTime = request.EndTime ?? auction.Info?.EndTime;

        if (!startTime.HasValue || !endTime.HasValue)
        {
            return Error.Validation(
                "Timing",
                "Auction.TimingRequired",
                "Auction timing must be defined before this update can be applied.");
        }

        var qualificationStart = request.QualificationStartAt ?? auction.Info?.Qualification?.StartTime;
        var qualificationEnd = request.QualificationEndAt ?? auction.Info?.Qualification?.EndTime;

        if (!qualificationStart.HasValue || !qualificationEnd.HasValue)
            return AuctionErrors.Auction.QualificationWindowRequired;

        var qualificationResult = QualificationWindow.Create(
            qualificationStart.Value,
            qualificationEnd.Value);

        if (qualificationResult.IsFailure)
            return qualificationResult.Error;

        var info = AuctionInfo.Create(
            nowUtc: nowUtc,
            startTime: startTime.Value,
            endTime: endTime.Value,
            autoExtend: request.AutoExtend ?? auction.Info?.AutoExtend ?? true,
            extensionMinutes: request.ExtensionMinutes ?? auction.Info?.ExtensionMinutes ?? 5,
            qualification: qualificationResult.Value);

        return info.IsFailure ? info.Error : info.Value;
    }
}



