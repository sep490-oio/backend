using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext;
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
    IDisputeEligibilityService eligibilityService,
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

        // Single source of truth: IDisputeEligibilityService resolves caller's role
        // (seller / winner / losing_bidder / observer). Returns null when the auction
        // does not exist. For auctions ResolveRoleAsync always classifies an existing
        // entity into one of the four roles, so a null return here is equivalent to
        // Auction.NotFound (kept as a safety net consistent with the prior 404 above).
        // Observer-driven rejections surface via the Domain matrix invariant in
        // Dispute.CreateCase (Dispute.RoleNotAllowed) with a consistent Validation type.
        var roleKey = await eligibilityService.ResolveRoleAsync(
            userId.Value,
            DisputeEligibilityRule.TargetAuction,
            request.AuctionId,
            cancellationToken);

        if (roleKey is null)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        // Auction-only timing gate (status whitelist).
        var timingOk = await eligibilityService.IsTimingAllowedAsync(
            DisputeEligibilityRule.TargetAuction,
            request.AuctionId,
            cancellationToken);

        if (!timingOk)
            return Error.Forbidden("Dispute.StatusNotAllowed", "Auction not in disputable status.");

        // Respondent derivation mirrors role intent:
        //   - Seller    → respondent = winner (if any)
        //   - Winner    → respondent = seller
        //   - Losing bidder / observer → no specific respondent (settlement disputes
        //     against the seller, but matrix limits losing_bidder to deposit/cancel
        //     cases that target seller-initiated outcomes; pass seller for symmetry).
        Guid? respondentUserId = roleKey switch
        {
            DisputeEligibilityRule.RoleSeller => auction.WinnerId?.Value,
            _ => sellerId.Value,
        };

        var snapshot = DisputeContextSnapshotBuilder.ForAuction(auction);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: DisputeEligibilityRule.TargetAuction,
            RoleKey: roleKey,
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
