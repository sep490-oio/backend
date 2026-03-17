using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Media;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.SendDisputeMessage;

public sealed record SendDisputeMessageCommand(
    Guid DisputeId,
    string? Message,
    IReadOnlyList<Guid>? MediaUploadIds = null,
    bool IsInternal = false) : ICommand<DisputeMessageDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return SendDisputeMessageCommand.Check()
            .WithOwnerName("SendDisputeMessage")
            .Field(DisputeId).NotEmptyGuid();
    }
}

internal sealed class SendDisputeMessageCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    UploadContextRegistry contextRegistry,
    IMediaRelocationService mediaRelocationService,
    DisputeAccessService accessService,
    IPublisher publisher,
    ILogger<SendDisputeMessageCommandHandler> logger)
    : ICommandHandler<SendDisputeMessageCommand, DisputeMessageDto>
{
    private const string DisputeAttachmentContext = "dispute_attachment";

    public async Task<Result<DisputeMessageDto, Error>> Handle(
        SendDisputeMessageCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var disputeId = DisputeId.From(request.DisputeId);
        var messageText = request.Message?.Trim();
        var mediaUploadIds = (request.MediaUploadIds ?? []).Distinct().Select(MediaUploadId.From).ToList();

        if (string.IsNullOrWhiteSpace(messageText) && mediaUploadIds.Count == 0)
        {
            return Error.Validation(
                "message",
                "Dispute.MessageOrAttachmentRequired",
                "Message text or at least one attachment is required.");
        }

        if (!string.IsNullOrWhiteSpace(messageText) && messageText.Length > 5000)
        {
            return Error.Validation(
                "message",
                "Dispute.MessageTooLong",
                "Message cannot be longer than 5000 characters.");
        }

        var accessResult = await accessService.GetAccessibleDisputeAsync(
            disputeId,
            cancellationToken: cancellationToken);

        if (accessResult.IsFailure)
            return accessResult.Error;

        var internalPermissionResult = accessService.EnsureInternalMessageAllowed(request.IsInternal);
        if (internalPermissionResult.IsFailure)
            return internalPermissionResult.Error;

        var dispute = accessResult.Value;

        var uploads = await LoadAndValidateUploadsAsync(mediaUploadIds, cancellationToken);
        if (uploads.IsFailure)
            return uploads.Error;

        var message = dispute.AddMessage(
            currentUser.UserId,
            messageText ?? string.Empty,
            nowUtc,
            request.IsInternal);

        var state = await accessService.EnsureParticipantStateAsync(dispute.Id, cancellationToken);
        state.MarkRead(message.Id, nowUtc);

        var attachmentDtos = new List<DisputeMessageAttachmentDto>(uploads.Value.Count);
        var attachments = new List<DisputeMessageAttachment>(uploads.Value.Count);

        for (var index = 0; index < uploads.Value.Count; index++)
        {
            var upload = uploads.Value[index];
            var attachment = DisputeMessageAttachment.Create(
                dispute.Id,
                message.Id,
                upload.Id,
                upload.StorageRef,
                upload.Info,
                index,
                nowUtc);

            attachments.Add(attachment);
            attachmentDtos.Add(ToAttachmentDto(attachment));
        }

        if (attachments.Count > 0)
            dbContext.InsertRange(attachments);

        foreach (var attachment in attachments)
        {
            var upload = uploads.Value.First(x => x.Id == attachment.MediaUploadId);
            var linkResult = upload.LinkToEntity(attachment.Id, nowUtc);
            if (linkResult.IsFailure)
                return linkResult.Error;

            await mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new DisputeMessageSentEvent(
                dispute.Id,
                message.Id,
                currentUser.UserId,
                request.IsInternal,
                nowUtc),
            cancellationToken);

        logger.LogInformation(
            "Dispute message {MessageId} created for dispute {DisputeId} by {UserId}. Internal={IsInternal}",
            message.Id.Value,
            dispute.Id.Value,
            currentUser.UserId.Value,
            request.IsInternal);

        return new DisputeMessageDto(
            message.Id.Value,
            dispute.Id.Value,
            currentUser.UserId.Value,
            currentUser.UserName?.Value ?? currentUser.UserId.Value.ToString(),
            message.Message,
            message.IsInternal,
            message.CreatedAt,
            attachmentDtos);
    }

    private async Task<Result<List<MediaUpload>, Error>> LoadAndValidateUploadsAsync(
        IReadOnlyCollection<MediaUploadId> mediaUploadIds,
        CancellationToken cancellationToken)
    {
        if (mediaUploadIds.Count == 0)
            return new List<MediaUpload>();

        var uploads = await dbContext.Set<MediaUpload>()
            .Where(x => mediaUploadIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != mediaUploadIds.Count)
        {
            var missingIds = mediaUploadIds
                .Where(id => uploads.All(upload => upload.Id != id))
                .Select(id => id.Value.ToString());
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        if (uploads.Any(x => x.UserId != currentUser.UserId))
            return MediaErrors.NotOwnedByUser(string.Join(", ", uploads.Where(x => x.UserId != currentUser.UserId).Select(x => x.Id.Value)));

        if (uploads.Any(x => !x.IsConfirmed))
            return MediaErrors.NotConfirm;

        if (uploads.Any(x => !string.Equals(x.Context, DisputeAttachmentContext, StringComparison.OrdinalIgnoreCase)))
            return MediaErrors.WrongContext("dispute attachments", contextRegistry.GetAllContext());

        if (uploads.Any(x => x.IsLinked))
            return MediaErrors.AlreadyLinked;

        return uploads;
    }

    private static DisputeMessageAttachmentDto ToAttachmentDto(DisputeMessageAttachment attachment)
    {
        return new DisputeMessageAttachmentDto(
            attachment.Id.Value,
            attachment.Info.FileName,
            attachment.Info.IsVideo ? "video" : "image",
            attachment.Info.SecureUrl ?? string.Empty,
            attachment.Info.Bytes ?? 0,
            attachment.Info.Format ?? string.Empty,
            attachment.Info.Width,
            attachment.Info.Height,
            attachment.Info.DurationSeconds);
    }
}

