using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.AddAdminDisputeMessage;

public sealed record AddAdminDisputeMessageCommand(
    Guid DisputeId,
    string Content,
    string Visibility = "external") : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddAdminDisputeMessageCommand.Check()
            .WithOwnerName("AddAdminDisputeMessage")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Content).NotWhiteSpace();
    }
}

internal sealed class AddAdminDisputeMessageCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddAdminDisputeMessageCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AddAdminDisputeMessageCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var isInternal = string.Equals(request.Visibility, "internal", StringComparison.OrdinalIgnoreCase);

        var msg = DisputeMessage.Create(
            disputeId,
            currentUser.UserId,
            request.Content,
            clock.UtcNow,
            isInternal,
            request.Visibility.ToLowerInvariant());

        dbContext.Insert(msg);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
