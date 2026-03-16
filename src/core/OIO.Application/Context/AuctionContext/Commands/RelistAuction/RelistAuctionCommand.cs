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
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RelistAuction;

public sealed record RelistAuctionCommand(
    Guid AuctionId,
    DateTime QualificationStartAt,
    DateTime QualificationEndAt,
    DateTime StartAt,
    DateTime EndAt,
    string? Reason = null) : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate() =>
        RelistAuctionCommand.Check()
            .WithOwnerName("RelistAuction")
            .Field(AuctionId).NotEmptyGuid();
}

internal sealed class RelistAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IAppConfigs appConfigs)
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

        if (auction.Status != OIO.Domain.Context.AuctionContext.Enums.AuctionStatus.PaymentDefaulted)
            return AuctionErrors.Auction.InvalidState(auction.Status.Id, "relist");

        if (auction.RelistHistories.Any(x => x.NewAuctionId.HasValue))
            return AuctionErrors.Auction.AlreadyRelisted;

        var qualification = QualificationWindow.Create(
            request.QualificationStartAt,
            request.QualificationEndAt);

        if (qualification.IsFailure)
            return qualification.Error;

        var info = AuctionInfo.Create(
            nowUtc: clock.UtcNow,
            startTime: request.StartAt,
            endTime: request.EndAt,
            autoExtend: auction.Info?.AutoExtend ?? true,
            extensionMinutes: auction.Info?.ExtensionMinutes ?? 5,
            qualification: qualification.Value);

        if (info.IsFailure)
            return info.Error;

        var createResult = Auction.Create(
            sellerId: auction.Item.SellerId,
            itemId: auction.ItemId,
            auctionType: auction.AuctionType!,
            pricing: auction.Pricing,
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
            await appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken));
    }
}
