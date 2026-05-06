using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.UnassignMonitoringAlert;

public sealed record UnassignMonitoringAlertCommand(Guid AlertId) : ICommand<MonitoringAlertDto>, IHasValidate
{
    public ViolationsError Validate() =>
        UnassignMonitoringAlertCommand.Check()
            .WithOwnerName("UnassignMonitoringAlert")
            .Field(AlertId).NotEmptyGuid();
}

internal sealed class UnassignMonitoringAlertCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ModerationAuditService auditService)
    : ICommandHandler<UnassignMonitoringAlertCommand, MonitoringAlertDto>
{
    public async Task<Result<MonitoringAlertDto, Error>> Handle(
        UnassignMonitoringAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Set<MonitoringAlert>()
            .FirstOrDefaultAsync(x => x.Id == MonitoringAlertId.From(request.AlertId), cancellationToken);

        if (alert is null)
            return Error.NotFound("MonitoringAlert.NotFound", "Monitoring alert was not found.");

        var oldState = new { assignedTo = alert.AssignedTo, assignedAt = alert.AssignedAt, status = alert.Status.Id };
        var unassignResult = alert.Unassign();
        if (unassignResult.IsFailure)
            return unassignResult.Error;

        auditService.Log(
            action: "monitoring_alert_unassigned",
            entityType: "MonitoringAlert",
            entityId: alert.Id.Value,
            oldData: oldState,
            newData: new { assignedTo = alert.AssignedTo, assignedAt = alert.AssignedAt, status = alert.Status.Id });

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return alert.ToDto();
    }
}
