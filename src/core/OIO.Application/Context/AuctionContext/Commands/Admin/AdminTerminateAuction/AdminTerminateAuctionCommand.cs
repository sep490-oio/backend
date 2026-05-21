using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.Admin.AdminTerminateAuction;

public sealed record AdminTerminateAuctionCommand(
    Guid AuctionId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate() =>
        AdminTerminateAuctionCommand.Check()
            .WithOwnerName("AdminTerminateAuction")
            .Field(AuctionId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
}

internal sealed class AdminTerminateAuctionCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IAuctionScheduler scheduler,
    IClock clock,
    IGrainFactory grainFactory)
    : ICommandHandler<AdminTerminateAuctionCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AdminTerminateAuctionCommand request,
        CancellationToken cancellationToken)
    {
        var grain = grainFactory.GetGrain<OIO.Domain.Context.AuctionContext.Grains.IAuctionGrain>(request.AuctionId);
        
        var grainResult = await grain.TerminateAuctionAsync(request.Reason, cancellationToken);
        if (grainResult.IsFailure) return grainResult.Error;

        await scheduler.CancelAsync(request.AuctionId, cancellationToken);

        return UnitResult.Success<Error>();
    }
}
