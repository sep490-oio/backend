using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

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
        await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext,
            _sender,
            _logger,
            Guid.Parse(notification.AuctionId),
            $"Auction cancelled: {notification.Reason}",
            cancellationToken);
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
        await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext,
            _sender,
            _logger,
            Guid.Parse(notification.AuctionId),
            $"Auction failed: {notification.Reason}",
            cancellationToken);
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
        await AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync(
            _dbContext,
            _sender,
            _logger,
            Guid.Parse(notification.AuctionId),
            "Auction sold: deposit returned to non-winning participants.",
            cancellationToken,
            Guid.Parse(notification.WinnerId));
    }
}
