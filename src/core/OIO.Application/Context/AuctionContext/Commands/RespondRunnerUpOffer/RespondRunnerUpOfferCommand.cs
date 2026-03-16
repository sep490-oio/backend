using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.RespondRunnerUpOffer;

public sealed record RespondRunnerUpOfferCommand(
    Guid AuctionId,
    bool Accept) : ICommand<WinnerOfferDto>, IHasValidate
{
    public ViolationsError Validate() =>
        RespondRunnerUpOfferCommand.Check()
            .WithOwnerName("RespondRunnerUpOffer")
            .Field(AuctionId).NotEmptyGuid();
}

internal sealed class RespondRunnerUpOfferCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
    : ICommandHandler<RespondRunnerUpOfferCommand, WinnerOfferDto>
{
    public async Task<Result<WinnerOfferDto, Error>> Handle(
        RespondRunnerUpOfferCommand request,
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

        var result = auction.RespondToRunnerUpOffer(
            currentUser.UserId,
            request.Accept,
            DateTime.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return result.Value.ToDto();
    }
}
