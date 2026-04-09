using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.AddBuyerDisputeMessage;

public sealed record AddBuyerDisputeMessageCommand(
    Guid DisputeId,
    string Content) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddBuyerDisputeMessageCommand.Check()
            .WithOwnerName("AddBuyerDisputeMessage")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Content).NotWhiteSpace();
    }
}

internal sealed class AddBuyerDisputeMessageCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddBuyerDisputeMessageCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AddBuyerDisputeMessageCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var userId = currentUser.UserId;
        if (dispute.ComplainantId != userId && dispute.RespondentId != userId)
            return Error.Forbidden("Dispute.Forbidden", "You are not allowed to access this dispute.");

        dispute.AddMessage(userId, request.Content, clock.UtcNow, isInternal: false);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
