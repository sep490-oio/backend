using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Settings;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.OfferRunnerUp;

public sealed record OfferRunnerUpCommand(Guid AuctionId)
    : ICommand<WinnerOfferDto>, IHasValidate
{
    public ViolationsError Validate() =>
        OfferRunnerUpCommand.Check()
            .WithOwnerName("OfferRunnerUp")
            .Field(AuctionId).NotEmptyGuid();
}

internal sealed class OfferRunnerUpCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ISystemSettingsService settings)
    : ICommandHandler<OfferRunnerUpCommand, WinnerOfferDto>
{
    public async Task<Result<WinnerOfferDto, Error>> Handle(
        OfferRunnerUpCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .Include(x => x.Bids)
            .Include(x => x.WinnerOffers)
            .FirstOrDefaultAsync(x => x.Id == auctionId, cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if (auction.Item.SellerId != currentUser.UserId)
            return AuctionErrors.Auction.OnlyOwnerCanCancel;

        var offerHours = await settings.GetAsync(
            SettingKeys.AuctionRunnerUpOfferExpirationHours,
            24,
            cancellationToken);

        var result = auction.OfferRunnerUp(DateTime.UtcNow, TimeSpan.FromHours(offerHours));
        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.ToDto();
    }
}
