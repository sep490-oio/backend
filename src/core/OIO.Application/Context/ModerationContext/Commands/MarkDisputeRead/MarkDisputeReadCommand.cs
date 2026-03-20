using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.MarkDisputeRead;

public sealed record MarkDisputeReadCommand(
    Guid DisputeId,
    Guid LastReadMessageId) : ICommand<DisputeParticipantReadStateDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return MarkDisputeReadCommand.Check()
            .WithOwnerName("MarkDisputeRead")
            .Field(DisputeId).NotEmptyGuid()
            .Field(LastReadMessageId).NotEmptyGuid();
    }
}

internal sealed class MarkDisputeReadCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    DisputeAccessService accessService,
    IPublisher publisher)
    : ICommandHandler<MarkDisputeReadCommand, DisputeParticipantReadStateDto>
{
    public async Task<Result<DisputeParticipantReadStateDto, Error>> Handle(
        MarkDisputeReadCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);
        var accessResult = await accessService.GetAccessibleDisputeAsync(
            disputeId,
            cancellationToken: cancellationToken);

        if (accessResult.IsFailure)
            return accessResult.Error;

        var messageId = DisputeMessageId.From(request.LastReadMessageId);
        var message = await dbContext.Set<DisputeMessage>()
            .FirstOrDefaultAsync(
                x => x.Id == messageId &&
                     x.DisputeId == disputeId &&
                     (accessService.CanViewInternalMessages || !x.IsInternal),
                cancellationToken);

        if (message is null)
        {
            return Error.NotFound(
                "Dispute.MessageNotFound",
                $"Message '{request.LastReadMessageId}' was not found in dispute '{request.DisputeId}'.");
        }

        var state = await accessService.EnsureParticipantStateAsync(disputeId, cancellationToken);
        var readAt = clock.UtcNow;
        state.MarkRead(message.Id, readAt);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new DisputeReadStateUpdatedEvent(disputeId, accessService.CurrentUserId, message.Id, readAt),
            cancellationToken);

        return new DisputeParticipantReadStateDto(
            disputeId.Value,
            accessService.CurrentUserId.Value,
            message.Id.Value,
            readAt);
    }
}
