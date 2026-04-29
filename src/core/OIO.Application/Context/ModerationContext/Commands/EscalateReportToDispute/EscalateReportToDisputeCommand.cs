using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.EscalateReportToDispute;

public sealed record EscalateReportToDisputeCommand(
    Guid ReportId,
    string? Title,
    string? DisputeType,
    string? Priority) : ICommand<DisputeThreadMetaDto>, IHasValidate
{
    public ViolationsError Validate() =>
        EscalateReportToDisputeCommand.Check()
            .WithOwnerName("EscalateReportToDispute")
            .Field(ReportId).NotEmptyGuid();
}

internal sealed class EscalateReportToDisputeCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ILogger<EscalateReportToDisputeCommandHandler> logger,
    ModerationAuditService auditService,
    IDisputeEligibilityService eligibilityService,
    IDisputeIntakeService intakeService)
    : ICommandHandler<EscalateReportToDisputeCommand, DisputeThreadMetaDto>
{
    public async Task<Result<DisputeThreadMetaDto, Error>> Handle(
        EscalateReportToDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Set<Report>()
            .FirstOrDefaultAsync(x => x.Id == ReportId.From(request.ReportId), cancellationToken);

        if (report is null)
            return Error.NotFound("Report.NotFound", "Report was not found.");

        if (report.Status == ReportStatus.Dismissed || report.Status == ReportStatus.Closed)
            return Error.Conflict("Report.CannotEscalate", "Cannot escalate a dismissed or closed report.");

        if (report.DisputeId is not null)
            return Error.Conflict("Report.AlreadyEscalated", "Report has already been escalated to a dispute.");

        var primaryTargetType = report.EntityType.ToLowerInvariant();
        UserId respondentId;
        AuctionId? auctionId = null;
        OrderId? orderId = null;

        switch (primaryTargetType)
        {
            case "order":
                var order = await dbContext.Set<Order>()
                    .FirstOrDefaultAsync(x => x.Id == OrderId.From(report.EntityId), cancellationToken);
                if (order is null)
                    return Error.NotFound("Order.NotFound", "Referenced order was not found.");
                respondentId = order.SellerId == report.ReporterId ? order.BuyerId : order.SellerId;
                orderId = order.Id;
                auctionId = order.AuctionId;
                break;

            case "auction":
                var auction = await dbContext.Set<Auction>()
                    .Include(a => a.Item)
                    .FirstOrDefaultAsync(x => x.Id == AuctionId.From(report.EntityId), cancellationToken);
                if (auction is null)
                    return Error.NotFound("Auction.NotFound", "Referenced auction was not found.");
                respondentId = auction.Item.SellerId;
                auctionId = auction.Id;
                break;

            default:
                return Error.Validation("entityType", "Report.UnsupportedEntityType",
                    $"Cannot escalate reports of entity type '{report.EntityType}' to disputes.");
        }

        // ── Map ReasonCode → (domain, caseType) ──
        // The escalation must produce a tuple consistent with DisputeEligibilityRule
        // so the matrix invariant in Dispute.CreateCase passes for the resolved role.
        var mapping = MapReasonCodeToCase(request.DisputeType ?? report.ReasonCode);
        if (mapping is null)
            return Error.Validation("reasonCode", "Report.UnsupportedReasonCode",
                $"Report reason '{report.ReasonCode}' cannot be mapped to a dispute case type.");

        var (domain, caseType) = mapping.Value;

        // ── Resolve reporter's role on the target via the canonical service ──
        // Closes the legacy admin-bypass: matrix invariant in Dispute.CreateCase will
        // reject role/domain/caseType combos that don't appear in DisputeEligibilityRule.
        var roleKey = await eligibilityService.ResolveRoleAsync(
            report.ReporterId.Value,
            primaryTargetType,
            report.EntityId,
            cancellationToken);

        if (roleKey is null)
            return Error.Validation("reporter", "Report.RoleNotResolved",
                "Reporter has no resolvable role on the reported entity (via report escalation).");

        var rawTitle = request.Title ?? $"Escalated from report #{report.Id.Value:N}";
        var title = rawTitle.Length > 40 ? rawTitle[..40] : rawTitle;
        var description = report.Description ?? $"Report reason: {report.ReasonCode}";

        // ── Route through IDisputeIntakeService → Dispute.CreateCase (matrix-enforced) ──
        var intakeResult = await intakeService.CreateDisputeAsync(
            new CreateDisputeRequest(
                Domain: domain,
                CaseType: caseType,
                PrimaryTargetType: primaryTargetType,
                RoleKey: roleKey,
                OrderId: orderId?.Value,
                AuctionId: auctionId?.Value,
                ShipmentId: null,
                WarehouseItemId: null,
                PaymentId: null,
                ComplainantUserId: report.ReporterId.Value,
                RespondentUserId: respondentId.Value,
                Title: title,
                Description: description,
                ContextSnapshotJson: null),
            cancellationToken);

        if (intakeResult.IsFailure)
        {
            var prefixed = Error.Validation(
                "escalation",
                intakeResult.Error.Code,
                $"{intakeResult.Error.Message} (via report escalation)");
            return prefixed;
        }

        var intake = intakeResult.Value;
        var disputeId = DisputeId.From(intake.Id);

        // Re-load aggregate to mark report.EscalateToDispute and emit ToMetaDto.
        var dispute = await dbContext.Set<Dispute>()
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null)
            return Error.NotFound("Dispute.NotFound", "Dispute was not found after intake.");

        var escalateResult = report.EscalateToDispute(dispute.Id, clock.UtcNow);
        if (escalateResult.IsFailure)
            return escalateResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditService.Log(
            action: "report_escalated_to_dispute",
            entityType: "Report",
            entityId: report.Id.Value,
            newData: new
            {
                reportId = report.Id.Value,
                disputeId = dispute.Id.Value,
                disputeNumber = dispute.DisputeNumber.Value
            });

        await NotificationDispatch.DispatchAsync(
            sender, logger,
            new CreateNotificationCommand(
                UserId: report.ReporterId.Value,
                NotificationType: "moderation",
                EventType: "report_escalated",
                Title: "Bao cao da duoc nang cap",
                Message: "Bao cao cua ban da duoc nang cap thanh tranh chap de xu ly chi tiet hon.",
                Priority: NotificationPriority.High,
                EntityType: "Dispute",
                EntityId: dispute.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    reportId = report.Id.Value,
                    disputeId = dispute.Id.Value
                })),
            cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender, logger,
            new CreateNotificationCommand(
                UserId: respondentId.Value,
                NotificationType: "moderation",
                EventType: "dispute_opened",
                Title: "Ban co tranh chap moi",
                Message: "Mot tranh chap lien quan den ban da duoc mo. Vui long xem va phan hoi.",
                Priority: NotificationPriority.High,
                EntityType: "Dispute",
                EntityId: dispute.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    disputeId = dispute.Id.Value
                })),
            cancellationToken);

        return dispute.ToMetaDto();
    }

    /// <summary>
    /// Maps the report's ReasonCode to a (domain, caseType) tuple recognised by
    /// <see cref="DisputeEligibilityRule"/>. Returns null when no safe mapping exists —
    /// callers must reject with <c>Report.UnsupportedReasonCode</c>.
    /// </summary>
    private static (string Domain, string CaseType)? MapReasonCodeToCase(string reasonCode) =>
        reasonCode.ToLowerInvariant() switch
        {
            "item_not_as_described" => ("item_condition", "not_as_described_after_delivery"),
            "item_damaged" => ("item_condition", "warehouse_damage"),
            "winner_non_payment" or "buyer_non_payment" or "buyer_fraud"
                => ("auction_settlement", "winner_non_payment"),
            "seller_non_fulfillment" => ("auction_settlement", "seller_non_fulfillment"),
            "package_not_received" => ("shipping", "package_not_received"),
            "damaged_package" => ("shipping", "damaged_package"),
            "wrong_item_received" => ("shipping", "wrong_item_received"),
            "missing_items" => ("shipping", "missing_items"),
            "duplicate_charge" => ("payment", "duplicate_charge"),
            "refund_missing" => ("payment", "refund_missing"),
            "authenticity_concern" or "counterfeit" or "suspicious_listing"
                => ("item_condition", "authenticity_concern"),
            _ => null,
        };
}
