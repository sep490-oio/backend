using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.RequestDisputeEvidence;

public sealed record RequestDisputeEvidenceCommand(
    Guid DisputeId,
    string Message) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RequestDisputeEvidenceCommand.Check()
            .WithOwnerName("RequestDisputeEvidence")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Message).NotWhiteSpace();
    }
}

internal sealed class RequestDisputeEvidenceCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<RequestDisputeEvidenceCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RequestDisputeEvidenceCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            queryBuilder: q => q.Include(d => d.StatusHistory),
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var transitionResult = dispute.TransitionTo(DisputeStatus.AwaitingEvidence, nowUtc);
        if (transitionResult.IsFailure) return transitionResult.Error;

        dispute.AddMessage(currentUser.UserId, request.Message, nowUtc, isInternal: false);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
