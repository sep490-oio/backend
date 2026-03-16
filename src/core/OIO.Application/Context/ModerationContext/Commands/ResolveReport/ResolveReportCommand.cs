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
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.ResolveReport;

public sealed record ResolveReportCommand(
    Guid ReportId,
    bool Dismissed,
    string? ResolutionNotes) : ICommand<ReportDto>, IHasValidate
{
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

        var oldState = new
        {
            status = report.Status.Id,
            resolutionNotes = report.ResolutionNotes
        };

        report.Resolve(request.ResolutionNotes, request.Dismissed, clock.UtcNow);
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
                    resolutionNotes = request.ResolutionNotes,
                    dismissed = request.Dismissed
                })),
            cancellationToken);

        return report.ToDto();
    }
}
