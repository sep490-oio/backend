using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RepairStuckInAuctionItems;

/// <summary>
/// One-off repair: finds items stuck in 'in_auction' whose latest auction is
/// cancelled / failed / terminated-without-sale and no other auction currently
/// blocks the item, and returns them to Active.
/// </summary>
public sealed record RepairStuckInAuctionItemsCommand() : ICommand<RepairStuckInAuctionItemsResponse>;

public sealed record RepairStuckInAuctionItemsResponse(
    int InspectedItems,
    int RepairedItems,
    IReadOnlyList<Guid> RepairedItemIds);

internal sealed class RepairStuckInAuctionItemsCommandHandler
    : ICommandHandler<RepairStuckInAuctionItemsCommand, RepairStuckInAuctionItemsResponse>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<RepairStuckInAuctionItemsCommandHandler> _logger;

    public RepairStuckInAuctionItemsCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<RepairStuckInAuctionItemsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<RepairStuckInAuctionItemsResponse, Error>> Handle(
        RepairStuckInAuctionItemsCommand request,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        var stuckItems = await _dbContext.Set<Item>()
            .Where(i => i.Status == ItemStatus.InAuction)
            .ToListAsync(cancellationToken);

        var repaired = new List<Guid>();

        foreach (var item in stuckItems)
        {
            var auctions = await _dbContext.Set<Auction>()
                .Where(a => a.ItemId == item.Id)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            if (auctions.Count == 0)
            {
                // No auction at all — safe to release.
                if (TryReturn(item, now))
                    repaired.Add(item.Id.Value);
                continue;
            }

            var latest = auctions[0];
            var latestIsTerminalWithoutSale =
                latest.Status == AuctionStatus.Cancelled ||
                latest.Status == AuctionStatus.Failed ||
                latest.Status == AuctionStatus.Terminated ||
                latest.Status == AuctionStatus.Ended;

            if (!latestIsTerminalWithoutSale)
                continue;

            var hasBlockingAuction = auctions.Any(a =>
                a.Status == AuctionStatus.Scheduled ||
                a.Status == AuctionStatus.Active ||
                a.Status == AuctionStatus.Sold);

            if (hasBlockingAuction)
                continue;

            if (TryReturn(item, now))
                repaired.Add(item.Id.Value);
        }

        if (repaired.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "RepairStuckInAuctionItems: Repaired {Count} items. Ids={Ids}",
                repaired.Count, string.Join(",", repaired));
        }

        return new RepairStuckInAuctionItemsResponse(
            InspectedItems: stuckItems.Count,
            RepairedItems: repaired.Count,
            RepairedItemIds: repaired);
    }

    private bool TryReturn(Item item, DateTime now)
    {
        var result = item.ReturnToActive(now);
        if (result.IsFailure)
        {
            _logger.LogWarning(
                "RepairStuckInAuctionItems: ReturnToActive failed for ItemId={ItemId}, Error={Error}",
                item.Id.Value, result.Error.Message);
            return false;
        }
        return true;
    }
}
