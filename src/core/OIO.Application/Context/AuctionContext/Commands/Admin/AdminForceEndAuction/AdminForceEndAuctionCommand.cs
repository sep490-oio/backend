using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainModels;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceEndAuction;

public sealed record AdminForceEndAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminForceEndAuctionCommand.Check()
            .WithOwnerName("AdminForceEndAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminForceEndAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ISealedBidEncryptionService sealedBidEncryptionService,
    IGrainFactory grainFactory,
    IClock clock)
    : ICommandHandler<AdminForceEndAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminForceEndAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Item)
                .Include(a => a.SealedBids)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Status != AuctionStatus.Active)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "force end");

        IReadOnlyCollection<RevealedSealedBidAmountGrain>? revealedSealedBids = null;

        if (auction.AuctionType == AuctionType.Sealed)
        {
            var revealedItems = new List<RevealedSealedBidAmountGrain>(auction.SealedBids.Count);

            foreach (var sealedBid in auction.SealedBids)
            {
                var decryptedAmount = sealedBidEncryptionService.Decrypt(sealedBid.AmountEncrypted);
                if (decryptedAmount.IsFailure)
                {
                    var cancelResult = auction.CancelAuction(
                        reason: $"[ADMIN ForceEnd] SealedBidDecryptionFailure: {decryptedAmount.Error.Code}",
                        nowUtc: clock.UtcNow);

                    if (cancelResult.IsFailure)
                        return cancelResult.Error;

                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    return UnitResult.Success<Error>();
                }

                var amount = Domain.Context.Shared.ValueObjects.Money.Create(
                    decryptedAmount.Value,
                    auction.Pricing.Currency);

                if (amount.IsFailure)
                    return amount.Error;

                revealedItems.Add(new RevealedSealedBidAmountGrain(
                    sealedBid.Id.Value,
                    MoneyGrain.From(amount.Value)));
            }

            revealedSealedBids = revealedItems;
        }

        // Delegate to the Orleans grain for proper End + Resolve orchestration
        var grain = grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        var result = await grain.EndAuctionAsync(
            null, // admin — no user id needed
            revealedSealedBids,
            cancellationToken);

        return result.IsFailure ? result.Error : UnitResult.Success<Error>();
    }
}
