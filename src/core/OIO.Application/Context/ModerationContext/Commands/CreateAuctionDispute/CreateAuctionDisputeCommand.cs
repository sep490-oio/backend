using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateAuctionDispute;

public sealed record CreateAuctionDisputeCommand(
    Guid AuctionId,
    string Domain,
    string CaseType,
    string Title,
    string Description) : ICommand<DisputeIntakeDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateAuctionDisputeCommand.Check()
            .WithOwnerName("CreateAuctionDispute")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Domain).NotEmpty()
            .Field(CaseType).NotEmpty()
            .Field(Title).NotEmpty()
            .Field(Description).NotEmpty();
}

internal sealed class CreateAuctionDisputeCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IDisputeIntakeService intakeService)
    : ICommandHandler<CreateAuctionDisputeCommand, DisputeIntakeDto>
{
    public async Task<Result<DisputeIntakeDto, Error>> Handle(
        CreateAuctionDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(x => x.Id == AuctionId.From(request.AuctionId), cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        var userId = currentUser.UserId;
        var sellerId = auction.Item.SellerId;

        // Any authenticated user may file a dispute/report against an auction listing:
        //   - Seller  → respondent = winner (bilateral payment/winner-behavior dispute)
        //   - Winner  → respondent = seller (bilateral payment/delivery dispute)
        //   - 3rd party (bidder, watcher, visitor) → respondent = seller
        //     (moderation report: counterfeit listing, misleading info, fraud, etc.)
        // Abuse mitigation: rely on rate-limiting + moderator triage, not gatekeeping.
        var respondentUserId = userId == sellerId
            ? auction.WinnerId?.Value
            : sellerId.Value;

        var snapshot = DisputeContextSnapshotBuilder.ForAuction(auction);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: "auction",
            OrderId: null,
            AuctionId: request.AuctionId,
            ShipmentId: null,
            WarehouseItemId: null,
            PaymentId: null,
            ComplainantUserId: userId.Value,
            RespondentUserId: respondentUserId,
            Title: request.Title,
            Description: request.Description,
            ContextSnapshotJson: snapshot), cancellationToken);
    }
}
