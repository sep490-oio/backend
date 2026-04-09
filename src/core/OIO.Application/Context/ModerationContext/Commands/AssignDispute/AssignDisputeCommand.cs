using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.AssignDispute;

public sealed record AssignDisputeCommand(
    Guid DisputeId,
    Guid AssignToUserId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AssignDisputeCommand.Check()
            .WithOwnerName("AssignDispute")
            .Field(DisputeId).NotEmptyGuid()
            .Field(AssignToUserId).NotEmptyGuid();
    }
}

internal sealed class AssignDisputeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock)
    : ICommandHandler<AssignDisputeCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AssignDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var result = dispute.Assign(request.AssignToUserId, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
