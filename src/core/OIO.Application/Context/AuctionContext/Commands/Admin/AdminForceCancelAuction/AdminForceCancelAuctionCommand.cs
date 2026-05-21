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
    IClock clock,
    IGrainFactory grainFactory)
    : ICommandHandler<AdminForceCancelAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminForceCancelAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var grain = grainFactory.GetGrain<OIO.Domain.Context.AuctionContext.Grains.IAuctionGrain>(request.AuctionId);
        
        var grainResult = await grain.ForceCancelAuctionAsync(request.Reason, cancellationToken);
        if (grainResult.IsFailure) return grainResult.Error;

        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(auctionId, cancellationToken: cancellationToken);
        
        if (auction is not null)
        {
            var item = await dbContext.GetByIdAsync<Item, ItemId>(auction.ItemId, cancellationToken: cancellationToken);
            if (item is not null)
            {
                var nowUtc = clock.UtcNow;
                var itemResult = item.ReturnToActive(nowUtc);
                if (itemResult.IsFailure)
                    return itemResult;
                
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        await scheduler.CancelAsync(request.AuctionId, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
