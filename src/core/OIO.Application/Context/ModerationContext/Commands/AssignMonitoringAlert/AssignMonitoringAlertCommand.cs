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

namespace OIO.Application.Context.ModerationContext.Commands.AssignMonitoringAlert;

public sealed record AssignMonitoringAlertCommand(
    Guid AlertId,
    Guid AssignToUserId) : ICommand<MonitoringAlertDto>, IHasValidate
{
    public ViolationsError Validate() =>
        AssignMonitoringAlertCommand.Check()
            .WithOwnerName("AssignMonitoringAlert")
            .Field(AlertId).NotEmptyGuid()
            .Field(AssignToUserId).NotEmptyGuid();
}

internal sealed class AssignMonitoringAlertCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ModerationAuditService auditService)
    : ICommandHandler<AssignMonitoringAlertCommand, MonitoringAlertDto>
{
    public async Task<Result<MonitoringAlertDto, Error>> Handle(
        AssignMonitoringAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Set<MonitoringAlert>()
            .FirstOrDefaultAsync(x => x.Id == MonitoringAlertId.From(request.AlertId), cancellationToken);

        if (alert is null)
            return Error.NotFound("MonitoringAlert.NotFound", "Monitoring alert was not found.");

        var assigneeExists = await dbContext.Set<User>()
            .AnyAsync(
                x => x.Id == UserId.From(request.AssignToUserId) && x.DeletedAt == null,
                cancellationToken);

        if (!assigneeExists)
            return Error.NotFound("User.NotFound", "Assigned user was not found.");

        var oldState = new { assignedTo = alert.AssignedTo, assignedAt = alert.AssignedAt, status = alert.Status.Id };
        var assignResult = alert.Assign(request.AssignToUserId, clock.UtcNow);
        if (assignResult.IsFailure)
            return assignResult.Error;

        auditService.Log(
            action: "monitoring_alert_assigned",
            entityType: "MonitoringAlert",
            entityId: alert.Id.Value,
            oldData: oldState,
            newData: new { assignedTo = alert.AssignedTo, assignedAt = alert.AssignedAt, status = alert.Status.Id });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return alert.ToDto();
    }
}
