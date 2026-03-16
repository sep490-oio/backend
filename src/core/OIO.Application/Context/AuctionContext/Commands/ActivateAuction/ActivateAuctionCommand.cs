using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
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
    private readonly IAuctionScheduler _scheduler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ActivateAuctionCommandHandler(
        IDbContext dbContext,
        IAuctionScheduler scheduler,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _dbContext = dbContext;
        _scheduler = scheduler;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<UnitResult<Error>> Handle(
        ActivateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
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
        
        var result = auction.Start(nowUtc);

        if (result.IsFailure)
            return result.Error;
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Schedule end job
        await _scheduler.ScheduleEndAsync(
            auction.Id.Value, 
            auction.Info.EndTime,
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
