using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.RejectDispute;

public sealed record RejectDisputeCommand(
    Guid DisputeId,
    string Reason) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RejectDisputeCommand.Check()
            .WithOwnerName("RejectDispute")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace();
    }
}

internal sealed class RejectDisputeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<RejectDisputeCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RejectDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            queryBuilder: q => q.Include(d => d.StatusHistory),
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var result = dispute.Reject(request.Reason, currentUser.UserId.Value, clock.UtcNow);
        if (result.IsFailure) return result.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
