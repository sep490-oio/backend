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

        // Do not release items for successfully-closed auctions (Sold or Completed).
        // When the sequence is Sold → Completed → Terminated, this handler fires on the
        // Terminated event and reads Status=Completed at that instant — we must still
        // skip item release because the item has already been sold.
        if (auction.Status.IsSuccessfullyClosed)
        {
            _logger.LogInformation(
                "AuctionTerminalItemRelease: skip AuctionId={AuctionId} because status is {Status} (successfully closed).",
                auctionId.Value, auction.Status.Id);
            return;
        }

        var item = await _dbContext.Set<Item>()
            .FirstOrDefaultAsync(i => i.Id == auction.ItemId, ct);

        if (item is null)
            return;

        var result = item.ReturnToActive(_clock.UtcNow);
        if (result.IsFailure)
        {
            // Bug #7 fix: previously logged at Warning and silently returned, hiding split-brain
            // (auction Cancelled/Failed/Terminated but item stuck in non-Active state — typically
            // because the item was concurrently marked Sold/Removed by another path). Item.Sold
            // and Item.Removed are terminal — retry can't undo them, so we log at Error level
            // for ops alerting and return. If retry would actually help (transient DB hiccup),
            // the outbox processor's own retry mechanism will re-deliver the event.
            _logger.LogError(
                "AuctionTerminalItemRelease: ReturnToActive failed for ItemId={ItemId}, AuctionId={AuctionId}, ItemStatus={ItemStatus}, Error={Error}. " +
                "Item is stuck in a non-Active state after the auction terminated — manual ops intervention required.",
                item.Id.Value, auctionId.Value, item.Status.Id, result.Error.Message);
            return;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "AuctionTerminalItemRelease: Item {ItemId} returned to Active after terminal auction {AuctionId}.",
            item.Id.Value, auctionId.Value);
    }
}
