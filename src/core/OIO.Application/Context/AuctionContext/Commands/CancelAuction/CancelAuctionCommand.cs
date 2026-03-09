using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.CancelAuction;

public sealed record CancelAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return CancelAuctionCommand.Check()
            .WithOwnerName("CancelAuction")
            .Field(AuctionId)
            .NotEmptyGuid()
            .Field(Reason)
            .NotWhiteSpace();
    }
}

internal sealed class CancelAuctionCommandHandler
    : ICommandHandler<CancelAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuctionScheduler _scheduler;
    private readonly IClock _clock;
    
    public CancelAuctionCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IAuctionScheduler scheduler,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _scheduler = scheduler;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        CancelAuctionCommand request,
        CancellationToken cancellationToken)
    {
        
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Bids)
                .Include(a => a.AutoBids)
                .Include(a => a.PriceHistories)
                .Include(a => a.Watchers)
                .AsSplitQuery(),
            cancellationToken: cancellationToken
            );

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.SellerId != _currentUser.UserId && !_currentUser.IsInRole(App.Roles.Catalogs.Admin))
            return AuctionErrors.Auction.OnlyOwnerCanCancel;

        var nowUtc = _clock.UtcNow;
        
        var result = auction.CancelAuction(request.Reason, nowUtc);

        if (result.IsFailure)
        {
            return result;
        }
        
        var item = await _dbContext.GetByIdAsync<Item, ItemId>(auction.ItemId, cancellationToken: cancellationToken);
        
        if (item is not null)
        {
            result = item.ReturnToActive(nowUtc);
            
            if (result.IsFailure)
            {
                return result;
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _scheduler.CancelAsync(auction.Id.Value, cancellationToken);

        return result;
    }
}