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

namespace OIO.Application.Context.AuctionContext.Commands.AdminRevealSealedBid;

public sealed record AdminRevealSealedBidCommand(
    Guid AuctionId,
    Guid SealedBidId) : ICommand<SealedBidDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AdminRevealSealedBidCommand.Check()
            .WithOwnerName("AdminRevealSealedBid")
            .Field(AuctionId).NotEmptyGuid()
            .Field(SealedBidId).NotEmptyGuid();
    }
}

internal sealed class AdminRevealSealedBidCommandHandler
    : ICommandHandler<AdminRevealSealedBidCommand, SealedBidDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AdminRevealSealedBidCommandHandler(
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
        AdminRevealSealedBidCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query.Include(a => a.SealedBids),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var result = auction.RevealSealedBid(
            SealedBidId.From(request.SealedBidId),
            _currentUser.UserId,
            _clock.UtcNow);

        if (result.IsFailure)
            return result.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Value.ToDto();
    }
}
