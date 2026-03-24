using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.DepositFromWallet;

public sealed record DepositFromWalletCommand(
    Guid AuctionId,
    decimal Amount,
    string Currency) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DepositFromWalletCommand.Check()
            .WithOwnerName("DepositFromWallet")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Amount).Positive()
            .Field(Currency).NotWhiteSpace();
    }
}

internal sealed class DepositFromWalletCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<DepositFromWalletCommand>
{
    public async Task<UnitResult<Error>> Handle(
        DepositFromWalletCommand request,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var userId = currentUser.UserId;
        var auctionId = AuctionId.From(request.AuctionId);

        // 1. Load auction with deposits and participants
        var auction = await dbContext.Set<Auction>()
            .Include(a => a.Item)
            .Include(a => a.Deposits)
            .Include(a => a.Participants)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        // 2. Validate auction state
        if (auction.Item.SellerId == userId)
            return AuctionErrors.Auction.SelfBid;

        if (auction.Status == AuctionStatus.Cancelled ||
            auction.Status == AuctionStatus.Ended ||
            auction.Status == AuctionStatus.Sold ||
            auction.Status == AuctionStatus.Failed ||
            auction.Status == AuctionStatus.Terminated)
        {
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "place deposit from wallet");
        }

        if (auction.Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!auction.Info.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;

        if (!auction.Info.IsQualificationOpen(now))
            return AuctionErrors.Participant.JoinWindowClosed;

        // 3. Check no existing held deposit
        if (auction.Deposits.Any(d => d.BidderId == userId && d.IsHeld))
            return Error.Conflict("AuctionDeposit.AlreadyHeld", "An active deposit already exists for this auction.");

        // 4. Validate currency matches auction
        if (!string.Equals(request.Currency, auction.Pricing.Currency.Id, StringComparison.OrdinalIgnoreCase))
            return Money.Errors.CurrencyMismatch(auction.Pricing.Currency.Id, request.Currency);

        // 5. Load wallet
        var wallet = await dbContext.Set<Wallet>()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet is null)
            return Error.NotFound("Wallet.NotFound", "Wallet not found.");

        // 6. Hold amount from wallet
        var holdResult = wallet.Hold(
            amount: request.Amount,
            transactionId: null,
            description: $"Auction deposit from wallet for auction {auctionId}",
            nowUtc: now);

        if (holdResult.IsFailure)
            return holdResult.Error;

        // 7. Create AuctionDeposit
        var (_, isAmountFailure, depositAmount, amountError) = Money.Create(request.Amount, auction.Pricing.Currency);
        if (isAmountFailure)
            return amountError;

        var depositResult = AuctionDeposit.Create(
            auctionId,
            userId,
            depositAmount,
            transactionId: null,
            now);

        if (depositResult.IsFailure)
            return depositResult.Error;

        // 8. Register participant
        var participantResult = auction.RegisterParticipantFromDeposit(userId, now);
        if (participantResult.IsFailure)
            return participantResult.Error;

        dbContext.Insert(depositResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UnitResult.Success<Error>();
    }
}
