using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.PublishAuction;

public sealed record PublishAuctionCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return PublishAuctionCommand.Check()
            .WithOwnerName("PublishAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class PublishAuctionCommandHandler
    : ICommandHandler<PublishAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IAuctionScheduler _scheduler;
    private readonly IClock _clock;

    public PublishAuctionCommandHandler(
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
        PublishAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(x => x.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        // Only the seller can publish their auction
        if (auction.Item.SellerId != _currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerCanPublish;
        
        var nowUtc = _clock.UtcNow;

        var result = auction.Publish(nowUtc);

        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _scheduler.ScheduleStartAsync(
            auction.Id.Value,
            auction.Info.StartTime,
            cancellationToken);

        return UnitResult.Success<Error>();
        
    }
}