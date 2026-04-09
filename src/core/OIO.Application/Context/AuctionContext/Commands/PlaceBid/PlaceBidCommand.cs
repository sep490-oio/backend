using System.Net;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.PlaceBid;

public sealed record PlaceBidCommand(
    Guid AuctionId,
    decimal Amount,
    string Currency,
    IPAddress? IpAddress) : ICommand<PlaceBidResultDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return PlaceBidCommand
            .Check()
            .WithOwnerName("PlaceBid")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(Amount)
            .NotDefault()
            .NonNegative()
            .Field(Currency)
            .NotWhiteSpace()
            .InSet(Domain.Context.Shared.Enums.Currency.All.Select(x => x.Id));
    }
}

internal sealed class PlaceBidCommandHandler
    : ICommandHandler<PlaceBidCommand, PlaceBidResultDto>
{
    private static readonly TimeSpan InvalidBidWindow = TimeSpan.FromMinutes(10);

    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IWinnerOrderProvisioner _winnerOrderProvisioner;
    private readonly ILogger<PlaceBidCommandHandler> _logger;

    public PlaceBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IRuntimeSettings runtimeSettings,
        IWinnerOrderProvisioner winnerOrderProvisioner,
        ILogger<PlaceBidCommandHandler> logger)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _runtimeSettings = runtimeSettings;
        _winnerOrderProvisioner = winnerOrderProvisioner;
        _logger = logger;
    }

    public async Task<Result<PlaceBidResultDto, Error>> Handle(
        PlaceBidCommand request,
        CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);

        var (_, isFailure, amount, error) = Money.Create(request.Amount, request.Currency);
        if (isFailure)
        {
            await TrackInvalidBidAttemptAsync(request, error, cancellationToken);
            return error;
        }

        // Capture snapshot before bid to compare afterwards
        var (_, snapshotBeforeFailure, snapshotBefore, snapshotBeforeError) =
            await grain.GetSnapshotAsync(cancellationToken);

        var bidCountBefore = snapshotBeforeFailure ? 0 : snapshotBefore.BidCount;

        (_, isFailure, var bid, error) = await grain.PlaceBidAsync(
            _currentUser.UserId.Value,
            MoneyGrain.From(amount),
            request.IpAddress,
            cancellationToken);

        if (isFailure)
        {
            await TrackInvalidBidAttemptAsync(request, error, cancellationToken);
            return error;
        }

        // Get snapshot after bid placement to determine cascade info
        var (_, snapshotAfterFailure, snapshotAfter, _) =
            await grain.GetSnapshotAsync(cancellationToken);

        var bidCountAfter = snapshotAfterFailure ? bidCountBefore + 1 : snapshotAfter.BidCount;
        var finalPrice = snapshotAfterFailure ? bid.Amount.Amount : snapshotAfter.CurrentPrice;
        var currentWinnerId = snapshotAfterFailure ? (Guid?)null : snapshotAfter.WinnerId;

        // Auto-bids cascaded = total new bids minus the original manual bid
        var autoBidsCascaded = Math.Max(0, bidCountAfter - bidCountBefore - 1);

        // The bidder was immediately outbid if the current winner is not the bidder
        var wasImmediatelyOutbid = currentWinnerId.HasValue &&
                                   currentWinnerId.Value != _currentUser.UserId.Value;

        var bidDto = new BidDto(
            Id: bid.Id,
            AuctionId: bid.AuctionId,
            BidderId: bid.BidderId,
            BidderDisplayName: null,
            Amount: bid.Amount.ToDto(),
            IsAutoBid: bid.IsAutoBid,
            Status: bid.Status,
            CreatedAt: bid.CreatedAt);

        // Buy-now cap detection: a manual bid >= buyNowPrice is capped inside the
        // domain aggregate to buyNowPrice. Use the pre-bid snapshot's buy-now price
        // and the raw request amount to detect the cap path so the FE can show a
        // dedicated modal and jump straight to checkout.
        var snapshotBuyNowPrice = snapshotBeforeFailure ? (decimal?)null : snapshotBefore.BuyNowPrice;
        var triggeredBuyNowCap = snapshotBuyNowPrice.HasValue
                                 && request.Amount >= snapshotBuyNowPrice.Value
                                 && !snapshotAfterFailure
                                 && string.Equals(snapshotAfter.Status, AuctionStatus.Sold.Id, StringComparison.OrdinalIgnoreCase)
                                 && snapshotAfter.WinnerId == _currentUser.UserId.Value;

        // Buy-now-price-hit-via-bid: when a bid sweeps through the buy-now threshold
        // the auction grain transitions to Sold immediately. Provision the winner
        // order eagerly so the FE doesn't have to wait for the AuctionSoldEvent
        // handler. Skipped when an active buy-now reservation exists (that flow
        // owns its own order via BuyNowReservationFinalizer).
        Guid? eagerOrderId = null;
        try
        {
            var auctionIdVo = AuctionId.From(request.AuctionId);
            var auctionEntity = await _dbContext.GetByIdAsync<Auction, AuctionId>(
                id: auctionIdVo,
                queryBuilder: q => q.AsNoTracking().Include(a => a.Item),
                cancellationToken: cancellationToken);

            if (auctionEntity is not null
                && auctionEntity.Status == AuctionStatus.Sold
                && auctionEntity.WinnerId is not null
                && auctionEntity.WinnerId == _currentUser.UserId
                && auctionEntity.GetActiveBuyNowReservation(DateTime.UtcNow) is null)
            {
                var provisionResult = await _winnerOrderProvisioner.EnsureAsync(
                    auctionId: request.AuctionId,
                    winnerId: _currentUser.UserId.Value,
                    sellerId: auctionEntity.Item.SellerId.Value,
                    finalPrice: finalPrice,
                    currency: request.Currency,
                    occurredAt: DateTime.UtcNow,
                    ct: cancellationToken);

                if (provisionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "PlaceBid: winner order provisioning failed for auction {AuctionId}. Error={Error}",
                        request.AuctionId,
                        provisionResult.Error.Message);
                }
                else
                {
                    eagerOrderId = provisionResult.Value.Id.Value;
                }
            }
        }
        catch (Exception ex)
        {
            // Order creation failure is recoverable by the AuctionSoldEvent handler.
            _logger.LogWarning(
                ex,
                "PlaceBid: winner order provisioning threw for auction {AuctionId}. Recoverable by background handler.",
                request.AuctionId);
        }

        return new PlaceBidResultDto(
            Bid: bidDto,
            AutoBidsCascaded: autoBidsCascaded,
            FinalPrice: finalPrice,
            WasImmediatelyOutbid: wasImmediatelyOutbid,
            TriggeredBuyNowCap: triggeredBuyNowCap,
            OrderId: eagerOrderId);
    }

    private async Task TrackInvalidBidAttemptAsync(
        PlaceBidCommand request,
        Error error,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var ipString = request.IpAddress?.ToString();

        _dbContext.Insert(AuditLog.Create(
            actorUserId: _currentUser.UserId,
            actorRole: "bidder",
            action: "invalid_bid_attempt",
            entityType: "Auction",
            entityId: request.AuctionId,
            oldData: null,
            newData: JsonSerializer.Serialize(new
            {
                auctionId = request.AuctionId,
                bidderId = _currentUser.UserId.Value,
                ipAddress = ipString,
                amount = request.Amount,
                currency = request.Currency,
                errorCode = error.Code,
                errorMessage = error.Message
            }),
            ipAddress: request.IpAddress,
            nowUtc: nowUtc));

        var threshold = _runtimeSettings.Monitoring.InvalidBidBurstThreshold;

        if (threshold <= 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var windowStart = nowUtc - InvalidBidWindow;
        var previousAttempts = await _dbContext.Set<AuditLog>()
            .AsNoTracking()
            .Where(x =>
                x.Action == "invalid_bid_attempt" &&
                x.EntityType == "Auction" &&
                x.EntityId == request.AuctionId &&
                x.CreatedAt >= windowStart &&
                (x.ActorUserId == _currentUser.UserId ||
                 (request.IpAddress != null && x.IpAddress == request.IpAddress)))
            .CountAsync(cancellationToken);

        var recentAttempts = previousAttempts + 1;

        if (recentAttempts >= threshold)
        {
            var openAlertPayloads = await _dbContext.Set<MonitoringAlert>()
                .AsNoTracking()
                .Where(
                    x => x.EntityType == "Auction" &&
                         x.EntityId == request.AuctionId &&
                         x.AlertType == "invalid_bid_burst" &&
                         x.Status == AlertStatus.Open &&
                         x.CreatedAt >= windowStart)
                .Select(x => x.Payload)
                .ToListAsync(cancellationToken);

            var bidderIdString = _currentUser.UserId.Value.ToString();
            var hasOpenAlert = openAlertPayloads.Any(payload =>
                payload.Contains(bidderIdString, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(ipString) &&
                 payload.Contains(ipString, StringComparison.OrdinalIgnoreCase)));

            if (!hasOpenAlert)
            {
                _dbContext.Insert(MonitoringAlert.Create(
                    entityType: "Auction",
                    entityId: request.AuctionId,
                    alertType: "invalid_bid_burst",
                    severity: AlertSeverity.High,
                    payload: JsonSerializer.Serialize(new
                    {
                        auctionId = request.AuctionId,
                        bidderId = _currentUser.UserId.Value,
                        ipAddress = ipString,
                        recentAttempts,
                        threshold,
                        windowMinutes = InvalidBidWindow.TotalMinutes,
                        lastErrorCode = error.Code
                    }),
                    nowUtc: nowUtc));
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
