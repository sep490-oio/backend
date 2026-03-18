using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CancelInvalidBid;

public sealed record CancelInvalidBidCommand(
    Guid AuctionId,
    Guid BidId,
    string? Reason) : ICommand<BidDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CancelInvalidBidCommand.Check()
            .WithOwnerName("CancelInvalidBid")
            .Field(AuctionId).NotEmptyGuid()
            .Field(BidId).NotEmptyGuid();
}

internal sealed class CancelInvalidBidCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ModerationAuditService auditService)
    : ICommandHandler<CancelInvalidBidCommand, BidDto>
{
    public async Task<Result<BidDto, Error>> Handle(
        CancelInvalidBidCommand request,
        CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Bids)
            .Include(x => x.PriceHistories)
            .FirstOrDefaultAsync(x => x.Id == AuctionId.From(request.AuctionId), cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        var result = auction.CancelBidByAdmin(BidId.From(request.BidId), clock.UtcNow);
        if (result.IsFailure)
            return result.Error;

        var alert = MonitoringAlert.Create(
            entityType: "Auction",
            entityId: auction.Id.Value,
            alertType: "invalid_bid_cancelled",
            severity: AlertSeverity.High,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                auctionId = auction.Id.Value,
                bidId = request.BidId,
                reason = request.Reason
            }),
            nowUtc: clock.UtcNow);

        dbContext.Insert(alert);
        auditService.Log(
            action: "invalid_bid_cancelled",
            entityType: "Auction",
            entityId: auction.Id.Value,
            newData: new
            {
                auctionId = auction.Id.Value,
                bidId = request.BidId,
                reason = request.Reason
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.ToDto();
    }
}
