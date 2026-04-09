using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.AddBuyerDisputeEvidence;

public sealed record AddBuyerDisputeEvidenceCommand(
    Guid DisputeId,
    IReadOnlyList<Guid> MediaUploadIds) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddBuyerDisputeEvidenceCommand.Check()
            .WithOwnerName("AddBuyerDisputeEvidence")
            .Field(DisputeId).NotEmptyGuid();
    }
}

internal sealed class AddBuyerDisputeEvidenceCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddBuyerDisputeEvidenceCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AddBuyerDisputeEvidenceCommand request,
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

        if (request.MediaUploadIds.Count == 0)
            return Error.Validation("mediaUploadIds", "Dispute.NoMediaUploads",
                "At least one media upload is required.");

        var uploadIds = request.MediaUploadIds.Distinct().Select(MediaUploadId.From).ToList();
        var uploads = await dbContext.Set<MediaUpload>()
            .Where(x => uploadIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != uploadIds.Count)
            return MediaErrors.NotFounds("Some media uploads not found.");

        if (uploads.Any(x => x.UserId != currentUser.UserId))
            return MediaErrors.NotOwnedByUser("Not all uploads belong to current user.");

        var nowUtc = clock.UtcNow;
        foreach (var upload in uploads)
        {
            var linkResult = upload.LinkToEntity(disputeId, nowUtc);
            if (linkResult.IsFailure)
                return linkResult.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
