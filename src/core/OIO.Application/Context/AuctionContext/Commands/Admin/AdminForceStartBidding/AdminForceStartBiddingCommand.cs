using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceStartBidding;

public sealed record AdminForceStartBiddingCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AdminForceStartBiddingCommand.Check()
            .WithOwnerName("AdminForceStartBidding")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class AdminForceStartBiddingCommandHandler
    : ICommandHandler<AdminForceStartBiddingCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OIO.Application.Abstractions.Scheduling.IAuctionScheduler _scheduler;
    private readonly IGrainFactory _grainFactory;

    public AdminForceStartBiddingCommandHandler(
        IDbContext dbContext,
        IClock clock,
        IUnitOfWork unitOfWork,
        OIO.Application.Abstractions.Scheduling.IAuctionScheduler scheduler,
        IGrainFactory grainFactory)
    {
        _dbContext = dbContext;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _scheduler = scheduler;
        _grainFactory = grainFactory;
    }

    public async Task<UnitResult<Error>> Handle(
        AdminForceStartBiddingCommand request,
        CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<OIO.Domain.Context.AuctionContext.Grains.IAuctionGrain>(request.AuctionId);
        
        var grainResult = await grain.ForceStartBiddingAsync(cancellationToken);
        if (grainResult.IsFailure) return grainResult.Error;

        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(auctionId, cancellationToken: cancellationToken);

        if (auction is not null && auction.Info is not null)
        {
            var startItem = await _dbContext.GetByIdAsync<OIO.Domain.Context.CatalogContext.Aggregates.Items.Item, OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId>(
                auction.ItemId,
                cancellationToken: cancellationToken);

            if (startItem is not null)
            {
                var nowUtc = _clock.UtcNow;
                var startItemSyncResult = startItem.MarkInAuction(nowUtc);
                if (startItemSyncResult.IsFailure)
                    return startItemSyncResult.Error;
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            await _scheduler.ScheduleEndAsync(auction.Id.Value, auction.Info.EndTime, cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}
