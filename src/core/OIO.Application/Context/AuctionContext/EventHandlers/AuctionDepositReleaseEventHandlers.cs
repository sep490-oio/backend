using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCancelledDepositReleaseEventHandler
    : INotificationHandler<AuctionCancelledEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionCancelledDepositReleaseEventHandler> _logger;

    public AuctionCancelledDepositReleaseEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionCancelledDepositReleaseEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionCancelledEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        var releasedBidderIds = await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext, _sender, _logger, auctionId,
            $"Auction cancelled: {notification.Reason}",
            cancellationToken);

        if (releasedBidderIds.Count == 0) return;

        var auctionTitle = await GetAuctionItemTitle(auctionId, cancellationToken);

        foreach (var userId in releasedBidderIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender, _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_cancelled_deposit_returned",
                    Title: "Tien coc da duoc hoan tra",
                    Message: $"Phien dau gia \"{auctionTitle}\" da huy. Tien coc cua ban da duoc hoan tra vao vi.",
                    Priority: NotificationPriority.High,
                    EntityType: "Auction",
                    EntityId: auctionId,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId,
                        reason = notification.Reason
                    })),
                cancellationToken);
        }

        _logger.LogInformation(
            "Deposit release notifications sent for cancelled auction {AuctionId}. Notified={Count} depositors.",
            auctionId, releasedBidderIds.Count);
    }

    private async Task<string> GetAuctionItemTitle(Guid auctionId, CancellationToken ct)
    {
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: q => q.AsNoTracking().Include(a => a.Item),
            cancellationToken: ct);
        return auction?.Item?.Title?.Value ?? "N/A";
    }
}

internal sealed class AuctionFailedDepositReleaseEventHandler
    : INotificationHandler<AuctionFailedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionFailedDepositReleaseEventHandler> _logger;

    public AuctionFailedDepositReleaseEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionFailedDepositReleaseEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionFailedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        var releasedBidderIds = await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext, _sender, _logger, auctionId,
            $"Auction failed: {notification.Reason}",
            cancellationToken);

        if (releasedBidderIds.Count == 0) return;

        var auctionTitle = await GetAuctionItemTitle(auctionId, cancellationToken);

        foreach (var userId in releasedBidderIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender, _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_failed_deposit_returned",
                    Title: "Tien coc da duoc hoan tra",
                    Message: $"Phien dau gia \"{auctionTitle}\" khong thanh cong. Tien coc cua ban da duoc hoan tra vao vi.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId),
                cancellationToken);
        }

        _logger.LogInformation(
            "Deposit release notifications sent for failed auction {AuctionId}. Notified={Count} depositors.",
            auctionId, releasedBidderIds.Count);
    }

    private async Task<string> GetAuctionItemTitle(Guid auctionId, CancellationToken ct)
    {
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: q => q.AsNoTracking().Include(a => a.Item),
            cancellationToken: ct);
        return auction?.Item?.Title?.Value ?? "N/A";
    }
}

internal sealed class AuctionSoldDepositReleaseEventHandler
    : INotificationHandler<AuctionSoldEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionSoldDepositReleaseEventHandler> _logger;

    public AuctionSoldDepositReleaseEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionSoldDepositReleaseEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionSoldEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);
        var winnerId = Guid.Parse(notification.WinnerId);

        var releasedBidderIds = await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext, _sender, _logger, auctionId,
            "Auction sold: deposit returned to non-winning participants.",
            cancellationToken,
            winnerId);

        if (releasedBidderIds.Count == 0) return;

        var auctionTitle = await GetAuctionItemTitle(auctionId, cancellationToken);

        foreach (var userId in releasedBidderIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender, _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_ended_deposit_returned",
                    Title: "Tien coc da duoc hoan tra",
                    Message: $"Phien dau gia \"{auctionTitle}\" da ket thuc. Tien coc cua ban da duoc hoan tra vao vi.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId),
                cancellationToken);
        }

        _logger.LogInformation(
            "Deposit release notifications sent for sold auction {AuctionId}. Notified={Count} non-winning depositors.",
            auctionId, releasedBidderIds.Count);
    }

    private async Task<string> GetAuctionItemTitle(Guid auctionId, CancellationToken ct)
    {
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: q => q.AsNoTracking().Include(a => a.Item),
            cancellationToken: ct);
        return auction?.Item?.Title?.Value ?? "N/A";
    }
}

internal sealed class AuctionTerminatedDepositReleaseEventHandler
    : INotificationHandler<AuctionTerminatedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionTerminatedDepositReleaseEventHandler> _logger;

    public AuctionTerminatedDepositReleaseEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionTerminatedDepositReleaseEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(AuctionTerminatedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = Guid.Parse(notification.AuctionId);

        var releasedBidderIds = await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext, _sender, _logger, auctionId,
            $"Auction terminated: {notification.Reason}",
            cancellationToken);

        if (releasedBidderIds.Count == 0) return;

        var auctionTitle = await GetAuctionItemTitle(auctionId, cancellationToken);

        foreach (var userId in releasedBidderIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender, _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_terminated_deposit_returned",
                    Title: "Tien coc da duoc hoan tra",
                    Message: $"Phien dau gia \"{auctionTitle}\" da bi cham dut. Tien coc cua ban da duoc hoan tra vao vi.",
                    Priority: NotificationPriority.High,
                    EntityType: "Auction",
                    EntityId: auctionId,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId,
                        reason = notification.Reason
                    })),
                cancellationToken);
        }

        _logger.LogInformation(
            "Deposit release notifications sent for terminated auction {AuctionId}. Notified={Count} depositors.",
            auctionId, releasedBidderIds.Count);
    }

    private async Task<string> GetAuctionItemTitle(Guid auctionId, CancellationToken ct)
    {
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            queryBuilder: q => q.AsNoTracking().Include(a => a.Item),
            cancellationToken: ct);
        return auction?.Item?.Title?.Value ?? "N/A";
    }
}
