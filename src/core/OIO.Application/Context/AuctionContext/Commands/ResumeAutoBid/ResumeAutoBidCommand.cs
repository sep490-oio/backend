using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Commands.ResumeAutoBid;

public sealed record ResumeAutoBidCommand(Guid AuctionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ResumeAutoBidCommand.Check()
            .WithOwnerName("ResumeAutoBid")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class ResumeAutoBidCommandHandler(
    IGrainFactory grainFactory,
    ICurrentUser currentUser)
    : ICommandHandler<ResumeAutoBidCommand>
{
    public async Task<UnitResult<Error>> Handle(
        ResumeAutoBidCommand request,
        CancellationToken cancellationToken)
    {
        var grain = grainFactory.GetGrain<IAuctionGrain>(request.AuctionId);
        var result = await grain.ResumeAutoBidAsync(currentUser.UserId.Value, cancellationToken);

        return result.IsFailure ? result.Error : UnitResult.Success<Error>();
    }
}
