using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminRelistAuction;

public sealed record AdminRelistAuctionCommand(
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
        AdminRelistAuctionCommand.Check()
            .WithOwnerName("AdminRelistAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(StartingPrice).WhenHasValue(x => x.NonNegative())
            .Field(BidIncrement).WhenHasValue(x => x.Positive())
            .Field(ReservePrice).WhenHasValue(x => x.NonNegative())
            .Field(BuyNowPrice).WhenHasValue(x => x.Positive())
            .Field(Currency).WhenHasValue(x => x.ExactLength(3));
}

internal sealed class AdminRelistAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IRuntimeSettings runtimeSettings)
    : ICommandHandler<AdminRelistAuctionCommand, AuctionDto>
{
    public async Task<Result<AuctionDto, Error>> Handle(
        AdminRelistAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.RelistHistories)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        // Admin bypass: no ownership check
        if (auction.Status != AuctionStatus.PaymentDefaulted &&
            auction.Status != AuctionStatus.Failed &&
            auction.Status != AuctionStatus.Cancelled &&
            auction.Status != AuctionStatus.Terminated)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "relist");

        if (auction.RelistHistories.Any(x => x.NewAuctionId.HasValue))
            return AuctionErrors.Auction.AlreadyRelisted;

        var qualification = QualificationWindow.Create(
            request.QualificationStartAt,
            request.QualificationEndAt);

        if (qualification.IsFailure)
            return qualification.Error;

        var autoExtend = auction.Info?.AutoExtend ?? true;
        if (auction.AuctionType == AuctionType.Sealed && autoExtend)
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
            buyNowPrice: request.BuyNowPrice ?? auction.Pricing.BuyNowAmount,
            isSealed: auction.AuctionType == AuctionType.Sealed);

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
        auction.RegisterRelist(relistedAuction.Id, request.Reason ?? "[ADMIN] Relisted by admin", clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return relistedAuction.ToDto(
            clock.UtcNow,
            runtimeSettings.Auction.ExtensionThreshold);
    }
}
