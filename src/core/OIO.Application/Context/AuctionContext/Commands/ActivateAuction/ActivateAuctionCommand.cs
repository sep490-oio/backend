using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ActivateAuction;

public sealed record ActivateAuctionCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ActivateAuctionCommand.Check()
            .WithOwnerName("ActivateAuction")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class ActivateAuctionCommandHandler
    : ICommandHandler<ActivateAuctionCommand>
{
    private readonly IDbContext _dbContext;
    private readonly AuctionActivationService _auctionActivationService;
    private readonly IClock _clock;

    public ActivateAuctionCommandHandler(
        IDbContext dbContext,
        AuctionActivationService auctionActivationService,
        IClock clock)
    {
        _dbContext = dbContext;
        _auctionActivationService = auctionActivationService;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ActivateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(x => x.Deposits)
                .Include(x => x.Participants)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        if(auction.Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!auction.Info.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;
        
        // Idempotent: skip if already activated
        if (auction.Status != AuctionStatus.Scheduled)
            return UnitResult.Success<Error>();

        var nowUtc = _clock.UtcNow;

        return await _auctionActivationService.ActivateScheduledAuctionAsync(
            auction,
            nowUtc,
            cancellationToken);
    }
}
