using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
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

namespace OIO.Application.Context.AuctionContext.Commands.SubmitSealedBid;

public sealed record SubmitSealedBidCommand(
    Guid AuctionId,
    string AmountEncrypted) : ICommand<SealedBidDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return SubmitSealedBidCommand.Check()
            .WithOwnerName("SubmitSealedBid")
            .Field(AuctionId).NotEmptyGuid()
            .Field(AmountEncrypted).NotWhiteSpace();
    }
}

internal sealed class SubmitSealedBidCommandHandler
    : ICommandHandler<SubmitSealedBidCommand, SealedBidDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public SubmitSealedBidCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<SealedBidDto, Error>> Handle(
        SubmitSealedBidCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Item)
                .Include(a => a.Participants)
                .Include(a => a.Deposits)
                .Include(a => a.SealedBids),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var result = auction.SubmitSealedBid(_currentUser.UserId, request.AmountEncrypted, _clock.UtcNow);
        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.ToDto();
    }
}
