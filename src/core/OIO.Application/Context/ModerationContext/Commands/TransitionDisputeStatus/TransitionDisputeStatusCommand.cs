using CSharpFunctionalExtensions;
using MediatR;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.TransitionDisputeStatus;

public sealed record TransitionDisputeStatusCommand(
    Guid DisputeId,
    string Status) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return TransitionDisputeStatusCommand.Check()
            .WithOwnerName("TransitionDisputeStatus")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Status).NotWhiteSpace();
    }
}

internal sealed class TransitionDisputeStatusCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IPublisher publisher)
    : ICommandHandler<TransitionDisputeStatusCommand>
{
    public async Task<UnitResult<Error>> Handle(
        TransitionDisputeStatusCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var newStatus = DisputeStatus.FromId(request.Status);
        if (newStatus.HasNoValue)
            return Error.Validation("status", "Dispute.InvalidStatus",
                $"Invalid status: {request.Status}");

        var result = dispute.TransitionTo(newStatus.Value, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new DisputeChangedEvent(dispute.Id, clock.UtcNow),
            cancellationToken);

        return UnitResult.Success<Error>();
    }
}
