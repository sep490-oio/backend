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

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminExtendAuctionTime;

public sealed record AdminExtendAuctionTimeCommand(
    Guid AuctionId,
    int ExtensionMinutes,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminExtendAuctionTimeCommand.Check()
            .WithOwnerName("AdminExtendAuctionTime")
            .Field(AuctionId).NotEmptyGuid()
            .Field(ExtensionMinutes).Positive()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminExtendAuctionTimeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IAuctionScheduler scheduler,
    IClock clock)
    : ICommandHandler<AdminExtendAuctionTimeCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminExtendAuctionTimeCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var extension = TimeSpan.FromMinutes(request.ExtensionMinutes);
        var result = auction.AdminExtendEndTime(extension, $"[ADMIN] {request.Reason}", clock.UtcNow);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reschedule the end job to match new end time
        if (auction.Info is not null)
            await scheduler.RescheduleEndAsync(auction.Id.Value, auction.Info.EndTime, cancellationToken);

        return result;
    }
}
