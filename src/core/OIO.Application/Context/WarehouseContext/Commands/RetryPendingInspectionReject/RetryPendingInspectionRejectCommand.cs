using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.RetryPendingInspectionReject;

/// <summary>
/// Admin-triggered recovery for <see cref="WarehouseInspection"/> rows whose
/// <c>WarehouseInspectionRejectedEvent</c> failed at the handler stage, commonly
/// because the return address could not be resolved from the original inbound
/// sender address or a legacy seller default address. After the admin fixes the
/// underlying issue, this command calls the same shared factory the event
/// handler uses to produce the <c>WarehouseToSellerShipment</c>.
/// </summary>
public sealed record RetryPendingInspectionRejectCommand(Guid InspectionId)
    : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RetryPendingInspectionRejectCommand.Check()
            .WithOwnerName("RetryPendingInspectionReject")
            .Field(InspectionId)
            .NotEmptyGuid();
    }
}

internal sealed class RetryPendingInspectionRejectCommandHandler(
    IDbContext dbContext,
    IWarehouseReturnShipmentFactory factory,
    IClock clock,
    ILogger<RetryPendingInspectionRejectCommandHandler> logger)
    : ICommandHandler<RetryPendingInspectionRejectCommand>
{
    public async Task<UnitResult<Error>> Handle(
        RetryPendingInspectionRejectCommand request,
        CancellationToken cancellationToken)
    {
        var inspectionId = WarehouseInspectionId.From(request.InspectionId);

        var inspection = await dbContext.Set<WarehouseInspection>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken);

        if (inspection is null)
            return WarehouseErrors.Inspection.NotFound(request.InspectionId.ToString());

        if (inspection.DecisionStatus != WarehouseInspectionDecisionStatus.Rejected)
            return Error.Conflict(
                "WarehouseInspection.NotRejected",
                "Retry is only available for inspections in the 'rejected' decision status.");

        var now = clock.UtcNow;
        var result = await factory.EnsureShipmentExistsAsync(
            inspectionId:    inspection.Id,
            warehouseItemId: inspection.WarehouseItemId,
            rejectionReason: inspection.DecisionReason ?? "Rejected by warehouse inspector",
            nowUtc:          now,
            cancellationToken: cancellationToken);

        switch (result.Outcome)
        {
            case EnsureShipmentOutcome.Created:
            case EnsureShipmentOutcome.CreatedViaDbDedup:
                logger.LogInformation(
                    "RetryPendingInspectionReject: shipment created/recovered for inspection {InspectionId}.",
                    request.InspectionId);
                return UnitResult.Success<Error>();

            case EnsureShipmentOutcome.AlreadyExists:
                logger.LogInformation(
                    "RetryPendingInspectionReject: shipment already exists for inspection {InspectionId} - no-op.",
                    request.InspectionId);
                return UnitResult.Success<Error>();

            default:
                logger.LogWarning(
                    "RetryPendingInspectionReject: factory returned {Outcome} for inspection {InspectionId}. Error={Error}",
                    result.Outcome,
                    request.InspectionId,
                    result.Error?.Message);
                return result.Error
                    ?? Error.Conflict(
                        "WarehouseInspection.RetryFailed",
                        $"Retry failed with outcome '{result.Outcome}'.");
        }
    }
}
