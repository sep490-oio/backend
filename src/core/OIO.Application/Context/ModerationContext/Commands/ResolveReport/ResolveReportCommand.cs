using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Commands.TriggerAuctionEmergency;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.ResolveReport;

public sealed record ResolveReportCommand(
    Guid ReportId,
    bool Dismissed,
    string? ResolutionNotes,
    string? EnforcementAction) : ICommand<ReportDto>, IHasValidate
{
    internal static readonly HashSet<string> ValidEnforcementActions =
        ["none", "warn_user", "suspend_listing", "remove_listing"];

    public ViolationsError Validate() =>
        ResolveReportCommand.Check()
            .WithOwnerName("ResolveReport")
            .Field(ReportId).NotEmptyGuid();
}

internal sealed class ResolveReportCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ILogger<ResolveReportCommandHandler> logger,
    ModerationAuditService auditService)
    : ICommandHandler<ResolveReportCommand, ReportDto>
{
    public async Task<Result<ReportDto, Error>> Handle(
        ResolveReportCommand request,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Set<Report>()
            .FirstOrDefaultAsync(x => x.Id == ReportId.From(request.ReportId), cancellationToken);

        if (report is null)
            return Error.NotFound("Report.NotFound", "Report was not found.");

        if (!string.IsNullOrEmpty(request.EnforcementAction)
            && !ResolveReportCommand.ValidEnforcementActions.Contains(request.EnforcementAction))
            return Error.Validation("enforcementAction", "EnforcementAction.Invalid",
                "Enforcement action must be one of: none, warn_user, suspend_listing, remove_listing.");

        var oldState = new
        {
            status = report.Status.Id,
            resolutionNotes = report.ResolutionNotes
        };

        var resolveResult = report.Resolve(request.ResolutionNotes, request.Dismissed, clock.UtcNow);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        auditService.Log(
            action: request.Dismissed ? "report_dismissed" : "report_resolved",
            entityType: "Report",
            entityId: report.Id.Value,
            oldData: oldState,
            newData: new
            {
                status = report.Status.Id,
                resolutionNotes = report.ResolutionNotes
            });

        // Enforcement actions (only when not dismissing)
        if (!request.Dismissed && !string.IsNullOrEmpty(request.EnforcementAction) && request.EnforcementAction != "none")
        {
            switch (request.EnforcementAction)
            {
                case "warn_user":
                    var ownerId = await ResolveEntityOwner(report.EntityType, report.EntityId, cancellationToken);
                    if (ownerId is not null)
                    {
                        await NotificationDispatch.DispatchAsync(
                            sender, logger,
                            new CreateNotificationCommand(
                                UserId: ownerId.Value.Value,
                                NotificationType: "moderation",
                                EventType: "moderation_warning",
                                Title: "Canh bao tu he thong",
                                Message: "Noi dung cua ban da nhan duoc bao cao vi pham. Vui long xem lai va dam bao tuan thu quy dinh.",
                                Priority: NotificationPriority.High,
                                EntityType: report.EntityType,
                                EntityId: report.EntityId,
                                Metadata: NotificationDispatch.SerializeMetadata(new
                                {
                                    reportId = report.Id.Value,
                                    enforcementAction = request.EnforcementAction
                                })),
                            cancellationToken);
                    }
                    break;

                case "suspend_listing":
                    if (string.Equals(report.EntityType, "auction", StringComparison.OrdinalIgnoreCase))
                    {
                        await sender.Send(
                            new TriggerAuctionEmergencyCommand(
                                AuctionId: report.EntityId,
                                TriggerSource: "enforcement_action",
                                Reason: $"Suspended by admin via report resolution. Notes: {request.ResolutionNotes}",
                                Payload: "{}"),
                            cancellationToken);
                    }
                    break;

                case "remove_listing":
                    if (string.Equals(report.EntityType, "auction", StringComparison.OrdinalIgnoreCase))
                    {
                        await sender.Send(
                            new TriggerAuctionEmergencyCommand(
                                AuctionId: report.EntityId,
                                TriggerSource: "enforcement_action",
                                Reason: $"Removed by admin via report resolution. Notes: {request.ResolutionNotes}",
                                Payload: "{}"),
                            cancellationToken);
                    }
                    break;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await NotificationDispatch.DispatchAsync(
            sender,
            logger,
            new CreateNotificationCommand(
                UserId: report.ReporterId.Value,
                NotificationType: "moderation",
                EventType: request.Dismissed ? "report_dismissed" : "report_resolved",
                Title: request.Dismissed ? "Bao cao da duoc dong" : "Bao cao da duoc xu ly",
                Message: request.Dismissed
                    ? "Bao cao cua ban da duoc xem xet va dong lai."
                    : "Bao cao cua ban da duoc xu ly boi he thong.",
                Priority: NotificationPriority.Normal,
                EntityType: "Report",
                EntityId: report.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    reportId = report.Id.Value,
                    dismissed = request.Dismissed
                })),
            cancellationToken);

        return report.ToDto();
    }

    private async Task<UserId?> ResolveEntityOwner(string entityType, Guid entityId, CancellationToken ct)
    {
        return entityType.ToLowerInvariant() switch
        {
            "order" => (await dbContext.Set<Order>().FirstOrDefaultAsync(x => x.Id == OrderId.From(entityId), ct))?.SellerId,
            "auction" => (await dbContext.Set<Auction>().Include(a => a.Item).FirstOrDefaultAsync(x => x.Id == AuctionId.From(entityId), ct))?.Item.SellerId,
            _ => null
        };
    }
}
