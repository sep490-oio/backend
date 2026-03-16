using System.Text;
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
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.AppDefinitions;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.CreateVerificationCorrectionDispute;

public sealed record CreateVerificationCorrectionDisputeCommand(
    Guid VerificationId,
    string Reason,
    string FullName,
    DateOnly DateOfBirth,
    string Gender,
    string IdType,
    string IdNumber,
    DateOnly? IdIssuedDate,
    DateOnly? IdExpiredDate,
    string? IdIssuedPlace,
    string FullAddress,
    string Province,
    string District,
    string Ward,
    string? Nationality = null,
    string? Message = null,
    IReadOnlyList<Guid>? MediaUploadIds = null) : ICommand<DisputeThreadMetaDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return CreateVerificationCorrectionDisputeCommand.Check()
            .WithOwnerName("CreateVerificationCorrectionDispute")
            .Field(VerificationId).NotEmptyGuid()
            .Field(Reason).NotWhiteSpace().MaxLength(1000)
            .Field(FullName).NotWhiteSpace().MaxLength(200)
            .Field(Gender).NotWhiteSpace()
                .InSet(OIO.Domain.Context.UserContext.Enums.Gender.All.Select(x => x.Id))
            .Field(IdType).NotWhiteSpace()
                .InSet(OIO.Domain.Context.UserContext.Enums.IdType.All.Select(x => x.Id))
            .Field(IdNumber).NotWhiteSpace().MaxLength(50)
            .Field(FullAddress).NotWhiteSpace().MaxLength(500)
            .Field(Province).NotWhiteSpace().MaxLength(100)
            .Field(District).NotWhiteSpace().MaxLength(100)
            .Field(Ward).NotWhiteSpace().MaxLength(100)
            .Field(Nationality).WhenHasValue(x => x.NotWhiteSpace().MaxLength(100))
            .Field(Message).WhenHasValue(x => x.MaxLength(5000));
    }
}

internal sealed class CreateVerificationCorrectionDisputeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    UploadContextRegistry contextRegistry,
    IMediaRelocationService mediaRelocationService,
    IPublisher publisher,
    ILogger<CreateVerificationCorrectionDisputeCommandHandler> logger)
    : ICommandHandler<CreateVerificationCorrectionDisputeCommand, DisputeThreadMetaDto>
{
    private const string DisputeAttachmentContext = "dispute_attachment";

    public async Task<Result<DisputeThreadMetaDto, Error>> Handle(
        CreateVerificationCorrectionDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.From(request.VerificationId);
        var nowUtc = clock.UtcNow;

        var verification = await dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null || verification.UserId != currentUser.UserId)
            return UserErrors.Verification.NotFound(verificationId);

        if (verification.Status != IdentityVerificationStatus.Approved || !verification.AutoVerified)
            return UserErrors.Verification.CorrectionDisputeRequiresAutoApprovedVerification;

        var hasOpenDispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .AnyAsync(
                x => x.VerificationId == verification.Id &&
                     x.Status != DisputeStatus.Resolved &&
                     x.Status != DisputeStatus.Closed &&
                     x.Status != DisputeStatus.Cancelled,
                cancellationToken);

        if (hasOpenDispute)
            return UserErrors.Verification.CorrectionDisputeAlreadyOpen;

        var respondentAdmin = await dbContext.Set<User>()
            .Include(x => x.Roles)
            .Where(x =>
                x.DeletedAt == null &&
                x.Status == UserStatus.Active &&
                x.Roles.Any(r => r.RoleName == App.Roles.Catalogs.Admin))
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (respondentAdmin is null)
            return UserErrors.Verification.NoAdminAvailable;

        var mediaUploadIds = (request.MediaUploadIds ?? [])
            .Distinct()
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await LoadAndValidateUploadsAsync(mediaUploadIds, cancellationToken);
        if (uploads.IsFailure)
            return uploads.Error;

        var createResult = Dispute.CreateForVerification(
            verificationId,
            complainantId: currentUser.UserId,
            respondentId: respondentAdmin.Id,
            type: DisputeType.VerificationCorrection,
            title: "KYC correction request",
            description: request.Reason.Trim(),
            nowUtc: nowUtc,
            priority: DisputePriority.Medium);

        if (createResult.IsFailure)
            return createResult.Error;

        var dispute = createResult.Value;
        var participantStates = new List<DisputeParticipantState>
        {
            DisputeParticipantState.Create(dispute.Id, currentUser.UserId, nowUtc),
            DisputeParticipantState.Create(dispute.Id, respondentAdmin.Id, nowUtc)
        };

        var messageBody = BuildInitialMessage(request);
        var message = dispute.AddMessage(currentUser.UserId, messageBody, nowUtc);
        participantStates[0].MarkRead(message.Id, nowUtc);

        var attachments = new List<DisputeMessageAttachment>(uploads.Value.Count);
        for (var index = 0; index < uploads.Value.Count; index++)
        {
            var upload = uploads.Value[index];
            attachments.Add(DisputeMessageAttachment.Create(
                dispute.Id,
                message.Id,
                upload.Id,
                upload.StorageRef,
                upload.Info,
                index,
                nowUtc));
        }

        dbContext.Insert(dispute);
        dbContext.InsertRange(participantStates);
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
                false,
                nowUtc),
            cancellationToken);

        logger.LogInformation(
            "Created verification correction dispute {DisputeId} for verification {VerificationId}.",
            dispute.Id.Value,
            verification.Id.Value);

        return dispute.ToMetaDto();
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
            return MediaErrors.WrongContext("dispute attachments", await contextRegistry.GetAllContextAsync(cancellationToken));

        if (uploads.Any(x => x.IsLinked))
            return MediaErrors.AlreadyLinked;

        return uploads;
    }

    private static string BuildInitialMessage(CreateVerificationCorrectionDisputeCommand request)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Verification correction request");
        builder.AppendLine($"Reason: {request.Reason.Trim()}");
        builder.AppendLine();
        builder.AppendLine("Corrected information:");
        builder.AppendLine($"- Full name: {request.FullName.Trim()}");
        builder.AppendLine($"- Date of birth: {request.DateOfBirth:yyyy-MM-dd}");
        builder.AppendLine($"- Gender: {request.Gender.Trim()}");
        builder.AppendLine($"- ID type: {request.IdType.Trim()}");
        builder.AppendLine($"- ID number: {request.IdNumber.Trim()}");

        if (request.IdIssuedDate.HasValue)
            builder.AppendLine($"- ID issued date: {request.IdIssuedDate:yyyy-MM-dd}");

        if (request.IdExpiredDate.HasValue)
            builder.AppendLine($"- ID expired date: {request.IdExpiredDate:yyyy-MM-dd}");

        if (!string.IsNullOrWhiteSpace(request.IdIssuedPlace))
            builder.AppendLine($"- ID issued place: {request.IdIssuedPlace.Trim()}");

        builder.AppendLine($"- Full address: {request.FullAddress.Trim()}");
        builder.AppendLine($"- Province: {request.Province.Trim()}");
        builder.AppendLine($"- District: {request.District.Trim()}");
        builder.AppendLine($"- Ward: {request.Ward.Trim()}");

        if (!string.IsNullOrWhiteSpace(request.Nationality))
            builder.AppendLine($"- Nationality: {request.Nationality.Trim()}");

        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            builder.AppendLine();
            builder.AppendLine("Additional note:");
            builder.AppendLine(request.Message.Trim());
        }

        return builder.ToString().Trim();
    }
}
