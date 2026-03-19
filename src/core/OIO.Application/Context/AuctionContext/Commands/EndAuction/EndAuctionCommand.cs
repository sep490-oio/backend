using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainModels;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.EndAuction;

public sealed record EndAuctionCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return EndAuctionCommand.Check()
            .WithOwnerName("EndAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class EndAuctionCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    ISealedBidEncryptionService sealedBidEncryptionService,
    IGrainFactory grainFactory)
    : ICommandHandler<EndAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        EndAuctionCommand request,
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

        if (currentUser.IsAuthenticated &&
            auction.Item.SellerId != currentUser.UserId &&
            !currentUser.IsInRole(App.Roles.Catalogs.Admin))
        {
            return Error.Forbidden(
                "Auction.OnlyOwnerCanClose",
                "Only the auction owner or an admin can close this auction.");
        }

        if (auction.Status != AuctionStatus.Active)
            return UnitResult.Success<Error>();

        IReadOnlyCollection<RevealedSealedBidAmountGrain>? revealedSealedBids = null;

        if (auction.AuctionType == AuctionType.Sealed)
        {
            var revealedItems = new List<RevealedSealedBidAmountGrain>(auction.SealedBids.Count);

            foreach (var sealedBid in auction.SealedBids)
            {
                var decryptedAmount = sealedBidEncryptionService.Decrypt(sealedBid.AmountEncrypted);
                if (decryptedAmount.IsFailure)
                    return decryptedAmount.Error;

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

        var grain = grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        var result = await grain.EndAuctionAsync(
            currentUser.IsAuthenticated ? currentUser.UserId.Value : null,
            revealedSealedBids,
            cancellationToken);

        return result.IsFailure ? result.Error : UnitResult.Success<Error>();
    }
}
