using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceCancelAuction;

public sealed record AdminForceCancelAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminForceCancelAuctionCommand.Check()
            .WithOwnerName("AdminForceCancelAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminForceCancelAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IAuctionScheduler scheduler,
    IClock clock)
    : ICommandHandler<AdminForceCancelAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminForceCancelAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Bids)
                .Include(a => a.AutoBids)
                .Include(a => a.PriceHistories)
                .Include(a => a.Watchers)
                .Include(a => a.Item)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = clock.UtcNow;

        var result = auction.CancelAuction($"[ADMIN] {request.Reason}", nowUtc, isAdminOverride: true);
        if (result.IsFailure)
            return result;

        var item = await dbContext.GetByIdAsync<Item, ItemId>(auction.ItemId, cancellationToken: cancellationToken);
        if (item is not null)
        {
            var itemResult = item.ReturnToActive(nowUtc);
            if (itemResult.IsFailure)
                return itemResult;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await scheduler.CancelAsync(auction.Id.Value, cancellationToken);

        return result;
    }
}
