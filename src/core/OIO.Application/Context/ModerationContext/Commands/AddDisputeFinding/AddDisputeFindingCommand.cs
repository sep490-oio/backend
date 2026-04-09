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

namespace OIO.Application.Context.ModerationContext.Commands.AddDisputeFinding;

public sealed record AddDisputeFindingReferenceRequest(
    string ReferenceType,
    Guid TargetId);

public sealed record AddDisputeFindingCommand(
    Guid DisputeId,
    string Domain,
    string Summary,
    string? VerdictRecommendation = null,
    string? FindingNote = null,
    List<AddDisputeFindingReferenceRequest>? References = null) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AddDisputeFindingCommand.Check()
            .WithOwnerName("AddDisputeFinding")
            .Field(DisputeId).NotEmptyGuid()
            .Field(Domain).NotWhiteSpace()
            .Field(Summary).NotWhiteSpace();
    }
}

internal sealed class AddDisputeFindingCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddDisputeFindingCommand>
{
    public async Task<UnitResult<Error>> Handle(
        AddDisputeFindingCommand request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);

        var dispute = await dbContext.GetByIdAsync<Dispute, DisputeId>(
            id: disputeId,
            cancellationToken: cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{disputeId}' not found.");

        var now = clock.UtcNow;

        var finding = DisputeFinding.Create(
            disputeId: disputeId,
            domain: request.Domain,
            authorUserId: currentUser.UserId,
            summary: request.Summary,
            nowUtc: now,
            verdictRecommendation: request.VerdictRecommendation,
            findingNote: request.FindingNote);

        dispute.AddFinding(finding);
        dbContext.Insert(finding);

        if (request.References is { Count: > 0 })
        {
            var targetIds = request.References.Select(r => r.TargetId).Distinct().ToHashSet();

            // Batch-load from the dispute's own data to avoid VO subquery issues.
            // Load all messages/attachments/evidence for this dispute, then filter
            // by targetId in memory (dataset is small per-dispute).
            var allMessages = await dbContext.Set<DisputeMessage>()
                .AsNoTracking()
                .Where(m => m.DisputeId == dispute.Id)
                .ToListAsync(cancellationToken);
            var messages = allMessages
                .Where(m => targetIds.Contains(m.Id.Value))
                .ToDictionary(m => m.Id.Value);

            var allAttachments = await dbContext.Set<DisputeMessageAttachment>()
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var attachments = allAttachments
                .Where(a => targetIds.Contains(a.Id.Value))
                .ToDictionary(a => a.Id.Value);

            var allEvidence = await dbContext.Set<DisputeEvidence>()
                .AsNoTracking()
                .Where(e => e.DisputeId == dispute.Id)
                .ToListAsync(cancellationToken);
            var evidence = allEvidence
                .Where(e => targetIds.Contains(e.Id.Value))
                .ToDictionary(e => e.Id.Value);

            foreach (var refReq in request.References)
            {
                var label = refReq.ReferenceType switch
                {
                    "message" when messages.TryGetValue(refReq.TargetId, out var msg) =>
                        (msg.Message.Length > 50 ? msg.Message[..50] : msg.Message),
                    "attachment" when attachments.TryGetValue(refReq.TargetId, out var att) =>
                        att.Info.FileName ?? "attachment",
                    "evidence" when evidence.TryGetValue(refReq.TargetId, out var ev) =>
                        ev.EvidenceInfo?.FileName ?? "evidence",
                    _ => refReq.TargetId.ToString()
                };

                finding.AddReference(refReq.ReferenceType, refReq.TargetId, label, now);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return UnitResult.Success<Error>();
    }
}
