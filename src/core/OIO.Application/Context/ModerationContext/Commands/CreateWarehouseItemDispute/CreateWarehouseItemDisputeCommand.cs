using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateWarehouseItemDispute;

public sealed record CreateWarehouseItemDisputeCommand(
    Guid WarehouseItemId,
    string Domain,
    string CaseType,
    string Title,
    string Description) : ICommand<DisputeIntakeDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateWarehouseItemDisputeCommand.Check()
            .WithOwnerName("CreateWarehouseItemDispute")
            .Field(WarehouseItemId).NotEmptyGuid()
            .Field(Domain).NotEmpty()
            .Field(CaseType).NotEmpty()
            .Field(Title).NotEmpty()
            .Field(Description).NotEmpty();
}

internal sealed class CreateWarehouseItemDisputeCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IDisputeEligibilityService eligibilityService,
    IDisputeIntakeService intakeService)
    : ICommandHandler<CreateWarehouseItemDisputeCommand, DisputeIntakeDto>
{
    public async Task<Result<DisputeIntakeDto, Error>> Handle(
        CreateWarehouseItemDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == WarehouseItemId.From(request.WarehouseItemId), cancellationToken);

        if (warehouseItem is null)
            return Error.NotFound("WarehouseItem.NotFound", "Warehouse item was not found.");

        var userId = currentUser.UserId;

        // Single source of truth: IDisputeEligibilityService resolves caller's role
        // (seller — derived via inbound shipment ownership). Returns null for non-sellers
        // — preserves the "WarehouseItem.NotOwner" error code for FE backward-compatibility.
        var roleKey = await eligibilityService.ResolveRoleAsync(
            userId.Value,
            DisputeEligibilityRule.TargetWarehouseItem,
            request.WarehouseItemId,
            cancellationToken);

        if (roleKey is null)
            return Error.Forbidden("WarehouseItem.NotOwner", "You are not the seller of this warehouse item.");

        var snapshot = DisputeContextSnapshotBuilder.ForWarehouseItem(warehouseItem);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: DisputeEligibilityRule.TargetWarehouseItem,
            RoleKey: roleKey,
            OrderId: null,
            AuctionId: null,
            ShipmentId: null,
            WarehouseItemId: request.WarehouseItemId,
            PaymentId: null,
            ComplainantUserId: userId.Value,
            RespondentUserId: null,
            Title: request.Title,
            Description: request.Description,
            ContextSnapshotJson: snapshot), cancellationToken);
    }
}
