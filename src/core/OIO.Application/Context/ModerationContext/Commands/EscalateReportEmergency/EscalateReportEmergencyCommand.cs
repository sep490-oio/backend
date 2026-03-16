using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.Commands.TriggerAuctionEmergency;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.EscalateReportEmergency;

public sealed record EscalateReportEmergencyCommand(
    Guid ReportId,
    string? ReasonOverride) : ICommand<ReportDto>, IHasValidate
{
    public ViolationsError Validate() =>
        EscalateReportEmergencyCommand.Check()
            .WithOwnerName("EscalateReportEmergency")
            .Field(ReportId).NotEmptyGuid();
}

internal sealed class EscalateReportEmergencyCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ISender sender,
    ModerationAuditService auditService)
    : ICommandHandler<EscalateReportEmergencyCommand, ReportDto>
{
    public async Task<Result<ReportDto, Error>> Handle(
        EscalateReportEmergencyCommand request,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Set<Report>()
            .FirstOrDefaultAsync(x => x.Id == ReportId.From(request.ReportId), cancellationToken);

        if (report is null)
            return Error.NotFound("Report.NotFound", "Report was not found.");

        if (!string.Equals(report.EntityType, "Auction", StringComparison.OrdinalIgnoreCase))
            return Error.Validation("entityType", "Report.UnsupportedEmergencyEntity", "Only auction reports can be escalated to emergency.");

        var emergencyReason = string.IsNullOrWhiteSpace(request.ReasonOverride)
            ? $"Escalated from report {report.Id.Value}: {report.ReasonCode}"
            : request.ReasonOverride;

        var triggerResult = await sender.Send(
            new TriggerAuctionEmergencyCommand(
                AuctionId: report.EntityId,
                TriggerSource: "report_escalation",
                Reason: emergencyReason,
                Payload: report.Attachments ?? "{}"),
            cancellationToken);

        if (triggerResult.IsFailure)
            return triggerResult.Error;

        report.MarkEscalated(clock.UtcNow);
        auditService.Log(
            action: "report_escalated_emergency",
            entityType: "Report",
            entityId: report.Id.Value,
            newData: new
            {
                status = report.Status.Id,
                escalatedEmergencyAt = report.EscalatedEmergencyAt,
                entityId = report.EntityId,
                reason = emergencyReason
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return report.ToDto();
    }
}
