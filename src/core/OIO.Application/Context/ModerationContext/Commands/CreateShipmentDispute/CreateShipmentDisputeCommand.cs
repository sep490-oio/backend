using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateShipmentDispute;

public sealed record CreateShipmentDisputeCommand(
    Guid ShipmentId,
    string Domain,
    string CaseType,
    string Title,
    string Description) : ICommand<DisputeIntakeDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateShipmentDisputeCommand.Check()
            .WithOwnerName("CreateShipmentDispute")
            .Field(ShipmentId).NotEmptyGuid()
            .Field(Domain).NotEmpty()
            .Field(CaseType).NotEmpty()
            .Field(Title).NotEmpty()
            .Field(Description).NotEmpty();
}

internal sealed class CreateShipmentDisputeCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IDisputeEligibilityService eligibilityService,
    IDisputeIntakeService intakeService)
    : ICommandHandler<CreateShipmentDisputeCommand, DisputeIntakeDto>
{
    public async Task<Result<DisputeIntakeDto, Error>> Handle(
        CreateShipmentDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == OutboundShipmentId.From(request.ShipmentId), cancellationToken);

        if (shipment is null)
            return Error.NotFound("Shipment.NotFound", "Outbound shipment was not found.");

        // Related order is needed for both respondent (seller) and snapshot.
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == shipment.OrderId, cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Related order was not found.");

        var userId = currentUser.UserId;

        // Single source of truth: IDisputeEligibilityService resolves caller's role
        // (buyer only — derived from the related order). Returns null for non-buyers
        // — preserves the "Shipment.NotBuyer" error code for FE backward-compatibility.
        var roleKey = await eligibilityService.ResolveRoleAsync(
            userId.Value,
            DisputeEligibilityRule.TargetShipment,
            request.ShipmentId,
            cancellationToken);

        if (roleKey is null)
            return Error.Forbidden("Shipment.NotBuyer", "You are not the buyer for this shipment.");

        var snapshot = DisputeContextSnapshotBuilder.ForOutboundShipment(shipment);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: DisputeEligibilityRule.TargetShipment,
            RoleKey: roleKey,
            OrderId: order.Id.Value,
            AuctionId: order.AuctionId.Value,
            ShipmentId: request.ShipmentId,
            WarehouseItemId: null,
            PaymentId: null,
            ComplainantUserId: userId.Value,
            RespondentUserId: order.SellerId.Value,
            Title: request.Title,
            Description: request.Description,
            ContextSnapshotJson: snapshot), cancellationToken);
    }
}
