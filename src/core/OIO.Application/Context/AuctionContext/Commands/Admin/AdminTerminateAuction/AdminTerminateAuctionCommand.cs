using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminTerminateAuction;

public sealed record AdminTerminateAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminTerminateAuctionCommand.Check()
            .WithOwnerName("AdminTerminateAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminTerminateAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IAuctionScheduler scheduler,
    IClock clock)
    : ICommandHandler<AdminTerminateAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminTerminateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Bids)
                .Include(a => a.AutoBids)
                .Include(a => a.Item)
                .Include(a => a.WinnerOffers)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var result = auction.Terminate($"[ADMIN] {request.Reason}", clock.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await scheduler.CancelAsync(auction.Id.Value, cancellationToken);

        return result;
    }
}
