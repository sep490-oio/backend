using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Commands.ReturnAuctionDeposit;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminRemoveBidWithRefund;

public sealed record AdminRemoveBidWithRefundCommand(
    Guid AuctionId,
    Guid BidId,
    string Reason) : ICommand<BidDto>, IHasValidate
{
    public ViolationsError Validate() =>
        AdminRemoveBidWithRefundCommand.Check()
            .WithOwnerName("AdminRemoveBidWithRefund")
            .Field(AuctionId).NotEmptyGuid()
            .Field(BidId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminRemoveBidWithRefundCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ISender sender,
    IClock clock,
    ILogger<AdminRemoveBidWithRefundCommandHandler> logger)
    : ICommandHandler<AdminRemoveBidWithRefundCommand, BidDto>
{
    public async Task<Result<BidDto, Error>> Handle(
        AdminRemoveBidWithRefundCommand request,
        CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Bids)
            .Include(x => x.PriceHistories)
            .Include(x => x.Deposits)
            .FirstOrDefaultAsync(x => x.Id == AuctionId.From(request.AuctionId), cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        // Find the bid to cancel and get the bidder's ID
        var bid = auction.Bids.FirstOrDefault(b => b.Id == BidId.From(request.BidId));
        if (bid is null)
            return Error.NotFound("Bid.NotFound", "Bid was not found.");

        var bidderId = bid.BidderId;

        // Cancel the bid
        var result = auction.CancelBidByAdmin(BidId.From(request.BidId), clock.UtcNow);
        if (result.IsFailure)
            return result.Error;

        // Create monitoring alert for audit
        var alert = MonitoringAlert.Create(
            entityType: "Auction",
            entityId: auction.Id.Value,
            alertType: "admin_bid_removed_with_refund",
            severity: AlertSeverity.High,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                auctionId = auction.Id.Value,
                bidId = request.BidId,
                bidderId = bidderId.Value,
                reason = request.Reason
            }),
            nowUtc: clock.UtcNow);

        dbContext.Insert(alert);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Refund the bidder's held deposit (if any)
        var heldDeposit = auction.Deposits.FirstOrDefault(d =>
            d.BidderId == bidderId && d.Status == Domain.Context.AuctionContext.Enums.DepositStatus.Held);

        if (heldDeposit is not null)
        {
            logger.LogInformation(
                "Returning deposit {DepositId} for bidder {BidderId} after admin bid removal on auction {AuctionId}",
                heldDeposit.Id.Value, bidderId.Value, auction.Id.Value);

            var refundResult = await sender.Send(
                new ReturnAuctionDepositCommand(heldDeposit.Id.Value, $"[ADMIN] Bid removed: {request.Reason}"),
                cancellationToken);

            if (refundResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to refund deposit {DepositId} for bidder {BidderId}: {Error}",
                    heldDeposit.Id.Value, bidderId.Value, refundResult.Error.Code);
            }
        }

        return result.Value.ToDto();
    }
}
