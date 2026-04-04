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
    ModerationAuditService auditService)
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

        UserId respondentId;
        AuctionId? auctionId = null;
        OrderId? orderId = null;

        switch (report.EntityType.ToLowerInvariant())
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

        if (auctionId is null)
            return Error.Validation("entityType", "Report.CannotCreateDispute",
                "Cannot create dispute without an auction reference.");

        var disputeType = MapReasonCodeToDisputeType(request.DisputeType ?? report.ReasonCode);
        var priority = MapPriority(request.Priority);
        var rawTitle = request.Title ?? $"Escalated from report #{report.Id.Value:N}";
        var title = rawTitle.Length > 40 ? rawTitle[..40] : rawTitle;
        var description = report.Description ?? $"Report reason: {report.ReasonCode}";

        var disputeResult = Dispute.Create(
            auctionId.Value,
            report.ReporterId,
            respondentId,
            disputeType,
            title,
            description,
            clock.UtcNow,
            DesiredResolution.NoAction,
            priority,
            orderId);

        if (disputeResult.IsFailure)
            return disputeResult.Error;

        var dispute = disputeResult.Value;

        var escalateResult = report.EscalateToDispute(dispute.Id, clock.UtcNow);
        if (escalateResult.IsFailure)
            return escalateResult.Error;

        dbContext.Insert(dispute);
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

    private static DisputeType MapReasonCodeToDisputeType(string reasonCode) =>
        reasonCode.ToLowerInvariant() switch
        {
            "item_not_as_described" => DisputeType.ItemNotAsDescribed,
            "item_damaged" => DisputeType.DamagedItem,
            "buyer_fraud" or "buyer_non_payment" => DisputeType.PaymentIssue,
            "suspicious_listing" or "counterfeit" => DisputeType.Counterfeit,
            _ => DisputeType.Other
        };

    private static DisputePriority MapPriority(string? priority) =>
        priority?.ToLowerInvariant() switch
        {
            "low" => DisputePriority.Low,
            "high" => DisputePriority.High,
            "urgent" => DisputePriority.Urgent,
            _ => DisputePriority.Medium
        };
}
