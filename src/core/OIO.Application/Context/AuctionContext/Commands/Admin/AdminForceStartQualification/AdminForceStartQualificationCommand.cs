using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminForceStartQualification;

public sealed record AdminForceStartQualificationCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AdminForceStartQualificationCommand.Check()
            .WithOwnerName("AdminForceStartQualification")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class AdminForceStartQualificationCommandHandler
    : ICommandHandler<AdminForceStartQualificationCommand>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly OIO.Application.Abstractions.Scheduling.IAuctionScheduler _scheduler;
    private readonly IGrainFactory _grainFactory;

    public AdminForceStartQualificationCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        OIO.Application.Abstractions.Scheduling.IAuctionScheduler scheduler,
        IGrainFactory grainFactory)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _scheduler = scheduler;
        _grainFactory = grainFactory;
    }

    public async Task<UnitResult<Error>> Handle(
        AdminForceStartQualificationCommand request,
        CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<OIO.Domain.Context.AuctionContext.Grains.IAuctionGrain>(request.AuctionId);
        
        var grainResult = await grain.ForceStartQualificationAsync(cancellationToken);
        if (grainResult.IsFailure) return grainResult.Error;

        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(auctionId, cancellationToken: cancellationToken);
        
        if (auction is not null && auction.Info is not null && auction.Info.HasQualification)
        {
            // Re-schedule based on the forced active qualification period
            await _scheduler.ScheduleQualificationCloseAsync(auction.Id.Value, auction.Info.Qualification!.EndTime, cancellationToken);
            await _scheduler.ScheduleStartAsync(auction.Id.Value, auction.Info.StartTime, cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}
