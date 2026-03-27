using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.UpdateWatcherPreferences;

public sealed record UpdateWatcherPreferencesCommand(
    Guid AuctionId,
    bool? NotifyOnBid = null,
    bool? NotifyOnEnd = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return UpdateWatcherPreferencesCommand.Check()
            .WithOwnerName("UpdateWatcherPreferences")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class UpdateWatcherPreferencesCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IClock clock,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateWatcherPreferencesCommand>
{
    public async Task<UnitResult<Error>> Handle(
        UpdateWatcherPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Watchers).Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = clock.UtcNow;
        var result = auction.UpdateWatcherPreferences(
            currentUser.UserId,
            nowUtc,
            request.NotifyOnBid,
            request.NotifyOnEnd);

        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result;
    }
}
