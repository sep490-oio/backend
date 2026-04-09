using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Queries.GetMyDisputeById;

public sealed record GetMyDisputeByIdQuery(Guid DisputeId) : IQuery<BuyerDisputeDetailDto>;

internal sealed class GetMyDisputeByIdQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetMyDisputeByIdQuery, BuyerDisputeDetailDto>
{
    public async Task<Result<BuyerDisputeDetailDto, Error>> Handle(
        GetMyDisputeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var disputeId = DisputeId.From(request.DisputeId);
        var userId = currentUser.UserId;

        var dispute = await dbContext.Set<Dispute>()
            .AsNoTracking()
            .Include(d => d.Messages).ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", $"Dispute '{request.DisputeId}' not found.");

        if (dispute.ComplainantId != userId && dispute.RespondentId != userId)
            return Error.Forbidden("Dispute.Forbidden", "You are not allowed to access this dispute.");

        // Batch-load display names for message authors (external only)
        var externalMessages = dispute.Messages
            .Where(m => m.Visibility == "external")
            .OrderBy(m => m.CreatedAt)
            .ToList();

        var authorIds = externalMessages
            .Select(m => m.SenderId)
            .Distinct()
            .ToList();

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id.Value, u => u.UserName.Value, cancellationToken);

        string DisplayName(Guid id) =>
            users.TryGetValue(id, out var name) ? name : id.ToString();

        var messages = externalMessages
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

        return new BuyerDisputeDetailDto(
            dispute.Id.Value,
            dispute.DisputeNumber.Value,
            dispute.Status.Id,
            dispute.CaseDomain,
            dispute.CaseType,
            dispute.Title,
            dispute.Description,
            dispute.CreatedAt,
            dispute.ModifiedAt,
            messages);
    }

    private static string ResolveResourceType(MediaInfo info)
    {
        if (info.IsVideo) return "video";
        if (info.IsImage) return "image";
        return "raw";
    }
}
