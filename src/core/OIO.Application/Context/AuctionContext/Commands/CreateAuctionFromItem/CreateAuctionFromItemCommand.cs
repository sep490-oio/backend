using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.AuctionContext.Commands.CreateAuctionFromItem;

public sealed record CreateAuctionFromItemCommand(
    Guid ItemId,
    decimal StartingPrice = 0,
    decimal BidIncrement = 0,
    decimal? ReservePrice = null,
    decimal? BuyNowPrice = null,
    int ExtensionMinutes = 5,
    string Currency = "VND",
    string AuctionType = "regular") : ICommand<AuctionDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateAuctionFromItemCommand.Check()
            .WithOwnerName("CreateAuctionFromItem")
            .Field(ItemId).NotEmptyGuid()
            .Field(StartingPrice).NonNegative()
            .Field(BidIncrement).NonNegative()
            .Field(ReservePrice).WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(BuyNowPrice).WhenHasValue(x => x.GreaterThanOrEqual(StartingPrice))
            .Field(ExtensionMinutes).BetweenInclusive(1, 30)
            .Field(Currency).NotWhiteSpace().ExactLength(3)
            .Field(AuctionType)
            .NotWhiteSpace()
            .InSet(Domain.Context.AuctionContext.Enums.AuctionType.All.Select(x => x.Id));
    }
}

internal sealed class CreateAuctionFromItemCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IRuntimeSettings runtimeSettings,
    AuctionDraftCreationService auctionDraftCreationService)
    : ICommandHandler<CreateAuctionFromItemCommand, AuctionDto>
{
    public async Task<Result<AuctionDto, Error>> Handle(
        CreateAuctionFromItemCommand request,
        CancellationToken cancellationToken)
    {
        var itemId = ItemId.From(request.ItemId);
        var item = await dbContext.GetByIdAsync<Item, ItemId>(
            id: itemId,
            queryBuilder: query => query.Include(x => x.Media),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(itemId);

        var createResult = await auctionDraftCreationService.CreateAsync(
            item,
            currentUser.UserId,
            new AuctionDraftCreationRequest(
                request.StartingPrice,
                request.BidIncrement,
                request.ReservePrice,
                request.BuyNowPrice,
                request.Currency,
                request.AuctionType),
            clock.UtcNow,
            cancellationToken);
        if (createResult.IsFailure)
            return createResult.Error;

        dbContext.Insert(createResult.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var savedAuction = await dbContext.Set<Auction>()
            .Include(x => x.Item)
            .FirstAsync(x => x.Id == createResult.Value.Id, cancellationToken);

        return savedAuction.ToDto(
            clock.UtcNow,
            runtimeSettings.Auction.ExtensionThreshold);
    }
}


