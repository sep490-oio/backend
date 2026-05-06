using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.ResolveMonitoringAlert;

public sealed record ResolveMonitoringAlertCommand(
    Guid AlertId,
    bool Ignored,
    string? Notes,
    string? ResolutionOutcome = null,
    string? ResolutionReason = null) : ICommand<MonitoringAlertDto>, IHasValidate
{
    public ViolationsError Validate() =>
        ResolveMonitoringAlertCommand.Check()
            .WithOwnerName("ResolveMonitoringAlert")
            .Field(AlertId).NotEmptyGuid();
}

internal sealed class ResolveMonitoringAlertCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    ModerationAuditService auditService)
    : ICommandHandler<ResolveMonitoringAlertCommand, MonitoringAlertDto>
{
    public async Task<Result<MonitoringAlertDto, Error>> Handle(
        ResolveMonitoringAlertCommand request,
        CancellationToken cancellationToken)
    {
        var alert = await dbContext.Set<MonitoringAlert>()
            .FirstOrDefaultAsync(x => x.Id == MonitoringAlertId.From(request.AlertId), cancellationToken);

        if (alert is null)
            return Error.NotFound("MonitoringAlert.NotFound", "Monitoring alert was not found.");

        var outcome = NormalizeOutcome(request.ResolutionOutcome, request.Ignored);
        var reason = request.ResolutionReason ?? request.Notes;
        var ignored = request.Ignored || outcome == "false_positive";

        var oldState = new
        {
            status = alert.Status.Id,
            notes = alert.Notes,
            resolutionOutcome = alert.ResolutionOutcome,
            resolutionReason = alert.ResolutionReason
        };

        var resolveResult = alert.Resolve(currentUser.UserId.Value, outcome, reason ?? string.Empty, ignored, clock.UtcNow);
        if (resolveResult.IsFailure)
            return resolveResult.Error;

        auditService.Log(
            action: ignored ? "monitoring_alert_ignored" : "monitoring_alert_resolved",
            entityType: "MonitoringAlert",
            entityId: alert.Id.Value,
            oldData: oldState,
            newData: new
            {
                status = alert.Status.Id,
                notes = alert.Notes,
                resolutionOutcome = alert.ResolutionOutcome,
                resolutionReason = alert.ResolutionReason
            });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return alert.ToDto();
    }

    private static string NormalizeOutcome(string? outcome, bool ignored)
    {
        if (!string.IsNullOrWhiteSpace(outcome))
            return outcome.Trim().ToLowerInvariant();

        return ignored ? "false_positive" : "valid_risk";
    }
}
