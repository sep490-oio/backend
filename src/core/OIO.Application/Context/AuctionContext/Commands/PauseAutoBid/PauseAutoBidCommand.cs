using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.PauseAutoBid;

public sealed record PauseAutoBidCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return PauseAutoBidCommand.Check()
            .WithOwnerName("PauseAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class PauseAutoBidCommandHandler(
    IGrainFactory grainFactory,
    ICurrentUser currentUser)
    : ICommandHandler<PauseAutoBidCommand>
{
    public async Task<UnitResult<Error>> Handle(
        PauseAutoBidCommand request,
        CancellationToken cancellationToken)
    {
        var grain = grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        var result = await grain.PauseAutoBidAsync(currentUser.UserId.Value, cancellationToken);

        return result.IsFailure ? result.Error : UnitResult.Success<Error>();
    }
}
