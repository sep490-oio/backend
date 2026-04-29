using CSharpFunctionalExtensions;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Services;

public sealed record CreateDisputeRequest(
    string Domain,
    string CaseType,
    string PrimaryTargetType,
    string RoleKey,
    Guid? OrderId,
    Guid? AuctionId,
    Guid? ShipmentId,
    Guid? WarehouseItemId,
    Guid? PaymentId,
    Guid ComplainantUserId,
    Guid? RespondentUserId,
    string Title,
    string Description,
    string? ContextSnapshotJson);

public interface IDisputeIntakeService
{
    Task<Result<DisputeIntakeDto, Error>> CreateDisputeAsync(
        CreateDisputeRequest request,
        CancellationToken ct);
}
