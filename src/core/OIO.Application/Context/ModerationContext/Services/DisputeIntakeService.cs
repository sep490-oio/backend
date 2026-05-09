using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Services;

internal sealed class DisputeIntakeService(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    IPublisher publisher) : IDisputeIntakeService
{
    public async Task<Result<DisputeIntakeDto, Error>> CreateDisputeAsync(
        CreateDisputeRequest request,
        CancellationToken ct)
    {
        // ── Deduplication ──
        // IsActive is a computed domain property EF cannot translate.
        // Inline the terminal-status check: active = NOT in {resolved, rejected, cancelled}.
        var hasDuplicate = await dbContext.Set<Dispute>()
            .AnyAsync(d =>
                d.CaseDomain == request.Domain &&
                d.PrimaryTargetType == request.PrimaryTargetType &&
                d.ComplainantId == UserId.From(request.ComplainantUserId) &&
                d.Status != DisputeStatus.Resolved &&
                d.Status != DisputeStatus.Rejected &&
                d.Status != DisputeStatus.Cancelled &&
                (request.OrderId == null || d.CaseOrderId == request.OrderId) &&
                (request.AuctionId == null || d.CaseAuctionId == request.AuctionId) &&
                (request.ShipmentId == null || d.ShipmentId == request.ShipmentId) &&
                (request.WarehouseItemId == null || d.WarehouseItemId == request.WarehouseItemId) &&
                (request.PaymentId == null || d.PaymentId == request.PaymentId),
            ct);

        if (hasDuplicate)
        {
            return Error.Conflict(
                "Dispute.DuplicateActive",
                "An active dispute already exists for this target.");
        }

        // ── Create ──
        var nowUtc = clock.UtcNow;
        var complainantId = UserId.From(request.ComplainantUserId);
        var respondentId = UserId.From(request.RespondentUserId ?? Guid.Empty);

        var disputeResult = Dispute.CreateCase(
            complainantId: complainantId,
            respondentId: respondentId,
            type: DisputeType.Other,
            title: request.Title.Length > 40 ? request.Title[..40] : request.Title,
            description: request.Description,
            nowUtc: nowUtc,
            roleKey: request.RoleKey,
            domain: request.Domain,
            caseType: request.CaseType,
            primaryTargetType: request.PrimaryTargetType,
            orderId: request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null,
            auctionId: request.AuctionId.HasValue ? AuctionId.From(request.AuctionId.Value) : null,
            shipmentId: request.ShipmentId,
            warehouseItemId: request.WarehouseItemId,
            paymentId: request.PaymentId,
            contextSnapshotJson: request.ContextSnapshotJson);

        if (disputeResult.IsFailure)
            return disputeResult.Error;

        var dispute = disputeResult.Value;
        dbContext.Insert(dispute);
        await unitOfWork.SaveChangesAsync(ct);

        await publisher.Publish(
            new DisputeChangedEvent(dispute.Id, clock.UtcNow),
            ct);

        return new DisputeIntakeDto(
            Id: dispute.Id.Value,
            DisputeNumber: dispute.DisputeNumber.Value,
            Status: dispute.Status.Id,
            Domain: dispute.CaseDomain,
            CaseType: dispute.CaseType,
            PrimaryTargetType: dispute.PrimaryTargetType,
            OrderId: dispute.CaseOrderId,
            AuctionId: dispute.CaseAuctionId,
            ShipmentId: dispute.ShipmentId,
            WarehouseItemId: dispute.WarehouseItemId,
            PaymentId: dispute.PaymentId,
            ComplainantUserId: dispute.ComplainantId.Value,
            RespondentUserId: dispute.RespondentId.Value == Guid.Empty ? null : dispute.RespondentId.Value,
            Title: dispute.Title,
            Description: dispute.Description,
            CreatedAt: dispute.CreatedAt);
    }
}
