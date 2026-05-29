using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.ProvisionWinnerOrder;

internal sealed class ProvisionWinnerOrderCommandHandler(
    IDbContext dbContext,
    IClock clock,
    IWinnerOrderProvisioner winnerOrderProvisioner)
    : ICommandHandler<ProvisionWinnerOrderCommand, Guid>
{
    public async Task<Result<Guid, Error>> Handle(
        ProvisionWinnerOrderCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Status != AuctionStatus.Sold && auction.Status != AuctionStatus.Completed)
            return Error.Conflict("Auction.NotClosed", "Auction must be successfully closed (Sold or Completed).");

        var winnerId = auction.WinnerId?.Value;
        if (winnerId is null)
        {
            // Fallback for buy now reservations? Buy Now also populates WinnerId when completed.
            return Error.Conflict("Auction.NoWinner", "Auction does not have a winner.");
        }

        var sellerId = auction.Item?.SellerId.Value;
        if (sellerId is null)
            return Error.Conflict("Auction.NoItem", "Auction does not have an associated item/seller.");

        if (!request.IsAdmin && request.CurrentUserId.HasValue && request.CurrentUserId.Value != sellerId.Value)
            return Error.Forbidden("Auction.NotSeller", "You are not authorized to provision an order for this auction.");

        var finalPrice = auction.Pricing.CurrentAmount;
        var currency = auction.Pricing.Currency.Id;
        var nowUtc = clock.UtcNow;

        var result = await winnerOrderProvisioner.EnsureAsync(
            auctionId: auctionId.Value,
            winnerId: winnerId.Value,
            sellerId: sellerId.Value,
            finalPrice: finalPrice,
            currency: currency,
            occurredAt: nowUtc,
            ct: cancellationToken);

        if (result.IsFailure)
            return result.Error;

        return result.Value.Id.Value;
    }
}
