using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// On any terminal-without-sale auction lifecycle event (cancelled / failed / terminated),
/// return the underlying item to Active so the seller can relist it.
/// This handler is dedicated to item state recovery; notification and deposit-release
/// handlers run independently.
/// </summary>
internal sealed class AuctionTerminalItemReleaseHandler :
    INotificationHandler<AuctionCancelledEvent>,
    INotificationHandler<AuctionFailedEvent>,
    INotificationHandler<AuctionTerminatedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<AuctionTerminalItemReleaseHandler> _logger;

    public AuctionTerminalItemReleaseHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AuctionTerminalItemReleaseHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public Task Handle(AuctionCancelledEvent n, CancellationToken ct) =>
        ReleaseItemAsync(n.AuctionId, ct);

    public Task Handle(AuctionFailedEvent n, CancellationToken ct) =>
        ReleaseItemAsync(n.AuctionId, ct);

    public Task Handle(AuctionTerminatedEvent n, CancellationToken ct) =>
        ReleaseItemAsync(n.AuctionId, ct);

    private async Task ReleaseItemAsync(string auctionIdStr, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(auctionIdStr));

        var auction = await _dbContext.Set<Auction>()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == auctionId, ct);

        if (auction is null)
            return;

        // Do not release items for sold auctions.
        if (auction.Status == AuctionStatus.Sold)
        {
            _logger.LogInformation(
                "AuctionTerminalItemRelease: skip AuctionId={AuctionId} because status is Sold.",
                auctionId.Value);
            return;
        }

        var item = await _dbContext.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == auction.ItemId, ct);

        if (item is null)
            return;

        var result = item.ReturnToActive(_clock.UtcNow);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "AuctionTerminalItemRelease: ReturnToActive failed for ItemId={ItemId}, AuctionId={AuctionId}, Error={Error}",
                item.Id.Value, auctionId.Value, result.Error.Message);
            return;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AuctionTerminalItemRelease: Item {ItemId} returned to Active after terminal auction {AuctionId}.",
            item.Id.Value, auctionId.Value);
    }
}
