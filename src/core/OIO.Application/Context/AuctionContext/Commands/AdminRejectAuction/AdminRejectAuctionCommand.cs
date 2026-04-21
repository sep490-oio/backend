using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.AdminRejectAuction;

/// <summary>
/// Bug #1 fix: admin endpoint to soft-reject (flag) a Pending/Approved/Scheduled auction.
/// Increments RejectionCount and raises AuctionRejectedEvent so the seller is notified.
/// Status is unchanged — seller can resubmit / fix / cancel as appropriate.
/// For terminal cancellation use CancelAuction (seller) or Terminate (admin emergency).
/// </summary>
public sealed record AdminRejectAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminRejectAuctionCommand.Check()
            .WithOwnerName("AdminRejectAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminRejectAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<AdminRejectAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminRejectAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = clock.UtcNow;

        var result = auction.MarkRejected(request.Reason, nowUtc);
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
