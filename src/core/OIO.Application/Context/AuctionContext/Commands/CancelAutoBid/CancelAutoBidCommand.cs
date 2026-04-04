using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.CancelAutoBid;

public sealed record CancelAutoBidCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return CancelAutoBidCommand.Check()
            .WithOwnerName("CancelAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class CancelAutoBidCommandHandler(
    IGrainFactory grainFactory,
    ICurrentUser currentUser)
    : ICommandHandler<CancelAutoBidCommand>
{
    public async Task<UnitResult<Error>> Handle(
        CancelAutoBidCommand request,
        CancellationToken cancellationToken)
    {
        var grain = grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        var result = await grain.CancelAutoBidAsync(currentUser.UserId.Value, cancellationToken);

        return result.IsFailure ? result.Error : UnitResult.Success<Error>();
    }
}
