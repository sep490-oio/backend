using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetAdminDisputeById;

public sealed record GetAdminDisputeByIdQuery(Guid DisputeId) : IQuery<AdminDisputeDetailDto>;

internal sealed class GetAdminDisputeByIdQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetAdminDisputeByIdQuery, AdminDisputeDetailDto>
{
    public async Task<Result<AdminDisputeDetailDto, Error>> Handle(
        GetAdminDisputeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .Include(d => d.Messages).ThenInclude(m => m.Attachments)
            .Include(d => d.Evidence)
            .Include(d => d.Findings).ThenInclude(f => f.References)
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{request.DisputeId}' not found.");

        // Collect all user IDs for batch loading
        var allUserIds = new HashSet<Guid>();
        allUserIds.Add(dispute.ComplainantId.Value);
        allUserIds.Add(dispute.RespondentId.Value);

        if (dispute.AssignedToUserId.HasValue)
            allUserIds.Add(dispute.AssignedToUserId.Value);
        if (dispute.ResolvedBy.HasValue)
            allUserIds.Add(dispute.ResolvedBy.Value);

        foreach (var msg in dispute.Messages)
            allUserIds.Add(msg.SenderId.Value);
        foreach (var finding in dispute.Findings)
            allUserIds.Add(finding.AuthorUserId.Value);

        var userIdList = allUserIds.Select(UserId.From).ToList();
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => userIdList.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id.Value, u => u.UserName.Value, cancellationToken);

        string DisplayName(Guid id) =>
            users.TryGetValue(id, out var name) ? name : id.ToString();

        string? DisplayNameNullable(Guid? id) =>
            id.HasValue && users.TryGetValue(id.Value, out var name) ? name : null;

        // Build lookup maps for finding references
        var allMessages = dispute.Messages.ToDictionary(m => m.Id.Value);
        var allAttachments = dispute.Messages
            .SelectMany(m => m.Attachments)
            .ToDictionary(a => a.Id.Value);
        var allEvidence = dispute.Evidence.ToDictionary(e => e.Id.Value);

        var messages = dispute.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new AdminDisputeMessageDto(
                m.Id.Value,
                DisplayName(m.SenderId.Value),
                m.Message,
                m.Visibility,
                m.CreatedAt,
                m.Attachments
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new AdminDisputeMessageAttachmentDto(
                        a.Id.Value,
                        a.Info.SecureUrl ?? string.Empty,
                        a.Info.FileName,
                        ResolveResourceType(a.Info),
                        a.Info.Format,
                        a.Info.Bytes,
                        a.Info.Width,
                        a.Info.Height,
                        (decimal?)a.Info.DurationSeconds))
                    .ToList()))
            .ToList();

        var evidence = dispute.Evidence
            .OrderBy(e => e.CreatedAt)
            .Select(e => new AdminDisputeEvidenceDto(
                e.Id.Value,
                e.SubmittedBy.Value,
                e.Type.Id,
                e.Description,
                e.EvidenceInfo?.SecureUrl,
                e.EvidenceInfo?.FileName,
                e.EvidenceInfo != null ? ResolveResourceType(e.EvidenceInfo) : null,
                e.EvidenceInfo?.Format,
                e.EvidenceInfo?.Bytes,
                e.EvidenceInfo?.Width,
                e.EvidenceInfo?.Height,
                (decimal?)e.EvidenceInfo?.DurationSeconds,
                e.CreatedAt))
            .ToList();

        var findings = dispute.Findings
            .OrderBy(f => f.CreatedAt)
            .Select(f => new AdminDisputeFindingDto(
                f.Id.Value,
                f.Domain,
                DisplayName(f.AuthorUserId.Value),
                f.VerdictRecommendation,
                f.Summary,
                f.FindingNote,
                f.CreatedAt,
                f.References
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => ResolveReferenceDto(r, allMessages, allAttachments, allEvidence, users))
                    .ToList()))
            .ToList();

        var isTerminal = dispute.Status.IsTerminal;
        var statusId = dispute.Status.Id;

        var canAssign = !isTerminal;
        var canRequestEvidence = dispute.Status.CanTransitionTo(DisputeStatus.AwaitingEvidence);
        var canAddFinding = !isTerminal;
        var canResolve = statusId is "under_review" or "awaiting_resolution_approval" or "awaiting_internal_review";
        var canReject = canResolve;

        var isAwaitingEvidence = statusId == "awaiting_evidence";
        var requestedEvidenceAt = isAwaitingEvidence ? dispute.ModifiedAt : null;
        var requestedEvidenceByDisplayName = isAwaitingEvidence
            ? DisplayNameNullable(dispute.AssignedToUserId)
            : null;

        return new AdminDisputeDetailDto(
            dispute.Id.Value,
            dispute.DisputeNumber.Value,
            dispute.Status.Id,
            dispute.CaseDomain,
            dispute.CaseType,
            dispute.PrimaryTargetType,
            dispute.Title,
            dispute.Description,
            dispute.ContextSnapshotJson,
            DisplayName(dispute.ComplainantId.Value),
            DisplayName(dispute.RespondentId.Value),
            DisplayNameNullable(dispute.AssignedToUserId),
            dispute.ResolutionOutcome,
            dispute.ResolutionReason,
            dispute.ResolutionActionSetJson,
            DisplayNameNullable(dispute.ResolvedBy),
            dispute.CaseResolvedAt ?? dispute.ResolvedAt,
            dispute.CaseOrderId ?? (dispute.OrderId.Value != Guid.Empty ? dispute.OrderId.Value : null),
            dispute.CaseAuctionId ?? dispute.AuctionId?.Value,
            dispute.ShipmentId,
            dispute.WarehouseItemId,
            dispute.PaymentId,
            dispute.AssignedToUserId,
            dispute.AssignedAt,
            dispute.CreatedAt,
            dispute.ModifiedAt,
            canAssign,
            canRequestEvidence,
            canAddFinding,
            canResolve,
            canReject,
            requestedEvidenceAt,
            requestedEvidenceByDisplayName,
            messages,
            evidence,
            findings);
    }

    private static AdminDisputeFindingReferenceDto ResolveReferenceDto(
        DisputeFindingReference reference,
        Dictionary<Guid, DisputeMessage> messagesMap,
        Dictionary<Guid, DisputeMessageAttachment> attachmentsMap,
        Dictionary<Guid, DisputeEvidence> evidenceMap,
        Dictionary<Guid, string> usersMap)
    {
        return reference.ReferenceType switch
        {
            "message" when messagesMap.TryGetValue(reference.TargetId, out var msg) =>
                new AdminDisputeFindingReferenceDto(
                    reference.ReferenceType,
                    reference.TargetId,
                    reference.LabelSnapshot,
                    null,
                    null,
                    msg.Message.Length > 120 ? msg.Message[..117] + "..." : msg.Message,
                    msg.CreatedAt),

            "attachment" when attachmentsMap.TryGetValue(reference.TargetId, out var att) =>
                new AdminDisputeFindingReferenceDto(
                    reference.ReferenceType,
                    reference.TargetId,
                    att.Info.FileName ?? reference.LabelSnapshot,
                    att.Info.SecureUrl,
                    ResolveResourceType(att.Info),
                    null,
                    att.CreatedAt),

            "evidence" when evidenceMap.TryGetValue(reference.TargetId, out var ev) =>
                new AdminDisputeFindingReferenceDto(
                    reference.ReferenceType,
                    reference.TargetId,
                    ev.EvidenceInfo?.FileName ?? reference.LabelSnapshot,
                    ev.EvidenceInfo?.SecureUrl,
                    ev.EvidenceInfo != null ? ResolveResourceType(ev.EvidenceInfo) : null,
                    null,
                    ev.CreatedAt),

            _ => new AdminDisputeFindingReferenceDto(
                reference.ReferenceType,
                reference.TargetId,
                reference.LabelSnapshot,
                null,
                null,
                null,
                reference.CreatedAt)
        };
    }

    private static string ResolveResourceType(MediaInfo info)
    {
        if (info.IsVideo) return "video";
        if (info.IsImage) return "image";
        return "raw";
    }
}
