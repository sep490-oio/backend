using System.Net;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
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
    IPAddress? IpAddress) : ICommand<BidDto>, IHasValidate
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
    : ICommandHandler<PlaceBidCommand, BidDto>
{
    private static readonly TimeSpan InvalidBidWindow = TimeSpan.FromMinutes(10);

    private readonly IGrainFactory _grainFactory;
    private readonly ICurrentUser _currentUser;
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuntimeSettings _runtimeSettings;

    public PlaceBidCommandHandler(
        IGrainFactory grainFactory,
        ICurrentUser currentUser,
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IRuntimeSettings runtimeSettings)
    {
        _grainFactory = grainFactory;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<BidDto, Error>> Handle(
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

        return new BidDto(
            Id: bid.Id,
            AuctionId: bid.AuctionId,
            BidderId: bid.BidderId,
            Amount: bid.Amount.ToDto(),
            IsAutoBid: bid.IsAutoBid,
            Status: bid.Status,
            CreatedAt: bid.CreatedAt);
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
