using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.AssignReport;

public sealed record AssignReportCommand(
    Guid ReportId,
    Guid AssignedToUserId) : ICommand<ReportDto>, IHasValidate
{
    public ViolationsError Validate() =>
        AssignReportCommand.Check()
            .WithOwnerName("AssignReport")
            .Field(ReportId).NotEmptyGuid()
            .Field(AssignedToUserId).NotEmptyGuid();
}

internal sealed class AssignReportCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ModerationAuditService auditService)
    : ICommandHandler<AssignReportCommand, ReportDto>
{
    public async Task<Result<ReportDto, Error>> Handle(
        AssignReportCommand request,
        CancellationToken cancellationToken)
    {
        var report = await dbContext.Set<Report>()
            .FirstOrDefaultAsync(x => x.Id == ReportId.From(request.ReportId), cancellationToken);

        if (report is null)
            return Error.NotFound("Report.NotFound", "Report was not found.");

        var assignee = await dbContext.Set<User>()
            .FirstOrDefaultAsync(x => x.Id == UserId.From(request.AssignedToUserId), cancellationToken);

        if (assignee is null)
            return Error.NotFound("User.NotFound", "Assigned admin was not found.");

        var oldState = new
        {
            assignedTo = report.AssignedTo?.Value,
            status = report.Status.Id
        };

        report.Assign(assignee.Id, clock.UtcNow);
        auditService.Log(
            action: "report_assigned",
            entityType: "Report",
            entityId: report.Id.Value,
            oldData: oldState,
            newData: new
            {
                assignedTo = assignee.Id.Value,
                status = report.Status.Id
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return report.ToDto();
    }
}
