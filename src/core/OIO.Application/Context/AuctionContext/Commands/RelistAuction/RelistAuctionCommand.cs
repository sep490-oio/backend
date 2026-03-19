using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RelistAuction;

public sealed record RelistAuctionCommand(
    Guid AuctionId,
    DateTime QualificationStartAt,
    DateTime QualificationEndAt,
    DateTime StartAt,
    DateTime EndAt,
    decimal? StartingPrice = null,
    decimal? BidIncrement = null,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    string? Currency = null,
    string? Reason = null) : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate() =>
        RelistAuctionCommand.Check()
            .WithOwnerName("RelistAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(StartingPrice).WhenHasValue(x => x.NonNegative())
            .Field(BidIncrement).WhenHasValue(x => x.Positive())
            .Field(ReservePrice).WhenHasValue(x => x.NonNegative())
            .Field(BuyNowPrice).WhenHasValue(x => x.Positive())
            .Field(Currency).WhenHasValue(x => x.ExactLength(3));
}

internal sealed class RelistAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IRuntimeSettings runtimeSettings)
    : ICommandHandler<RelistAuctionCommand, AuctionDto>
{
    public async Task<Result<AuctionDto, Error>> Handle(
        RelistAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.RelistHistories)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerCanCancel;

        if (auction.Status != Domain.Context.AuctionContext.Enums.AuctionStatus.PaymentDefaulted)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "relist");

        if (auction.RelistHistories.Any(x => x.NewAuctionId.HasValue))
            return AuctionErrors.Auction.AlreadyRelisted;

        var qualification = QualificationWindow.Create(
            request.QualificationStartAt,
            request.QualificationEndAt);

        if (qualification.IsFailure)
            return qualification.Error;

        var autoExtend = auction.Info?.AutoExtend ?? true;
        if (auction.AuctionType == Domain.Context.AuctionContext.Enums.AuctionType.Sealed && autoExtend)
        {
            return Error.Validation(
                "AutoExtend",
                "Auction.SealedAutoExtendNotSupported",
                "Sealed auctions do not support auto-extend.");
        }

        var info = AuctionInfo.Create(
            nowUtc: clock.UtcNow,
            startTime: request.StartAt,
            endTime: request.EndAt,
            autoExtend: autoExtend,
            extensionMinutes: auction.Info?.ExtensionMinutes ?? 5,
            qualification: qualification.Value);

        if (info.IsFailure)
            return info.Error;

        var currency = Currency.FromId(request.Currency ?? auction.Pricing.Currency.Id);
        if (currency.HasNoValue)
            return Currency.Errors.NotSupported;

        var pricing = AuctionPricing.Create(
            startingPrice: request.StartingPrice ?? auction.Pricing.StartingAmount,
            bidIncrement: request.BidIncrement ?? auction.Pricing.BidIncrementAmount,
            currency: currency.Value,
            reservePrice: request.ReservePrice ?? auction.Pricing.ReserveAmount,
            buyNowPrice: request.BuyNowPrice ?? auction.Pricing.BuyNowAmount);

        if (pricing.IsFailure)
            return pricing.Error;

        var createResult = Auction.Create(
            sellerId: auction.Item.SellerId,
            itemId: auction.ItemId,
            auctionType: auction.AuctionType!,
            pricing: pricing.Value,
            nowUtc: clock.UtcNow,
            info: info.Value);

        if (createResult.IsFailure)
            return createResult.Error;

        var relistedAuction = createResult.Value;
        dbContext.Insert(relistedAuction);
        auction.RegisterRelist(relistedAuction.Id, request.Reason, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return relistedAuction.ToDto(
            clock.UtcNow,
            runtimeSettings.Auction.ExtensionThreshold);
    }
}
