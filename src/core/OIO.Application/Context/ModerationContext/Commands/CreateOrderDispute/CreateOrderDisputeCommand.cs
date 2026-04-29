using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.ModerationContext;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ModerationContext.Commands.CreateOrderDispute;

public sealed record CreateOrderDisputeCommand(
    Guid OrderId,
    string Domain,
    string CaseType,
    string Title,
    string Description) : ICommand<DisputeIntakeDto>, IHasValidate
{
    public ViolationsError Validate() =>
        CreateOrderDisputeCommand.Check()
            .WithOwnerName("CreateOrderDispute")
            .Field(OrderId).NotEmptyGuid()
            .Field(Domain).NotEmpty()
            .Field(CaseType).NotEmpty()
            .Field(Title).NotEmpty()
            .Field(Description).NotEmpty();
}

internal sealed class CreateOrderDisputeCommandHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    IDisputeEligibilityService eligibilityService,
    IDisputeIntakeService intakeService)
    : ICommandHandler<CreateOrderDisputeCommand, DisputeIntakeDto>
{
    public async Task<Result<DisputeIntakeDto, Error>> Handle(
        CreateOrderDisputeCommand request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order was not found.");

        var userId = currentUser.UserId;

        // Single source of truth: IDisputeEligibilityService resolves caller's role
        // (buyer / seller). Returns null for non-participants — preserves the
        // "Order.NotParticipant" error code for FE backward-compatibility.
        var roleKey = await eligibilityService.ResolveRoleAsync(
            userId.Value,
            DisputeEligibilityRule.TargetOrder,
            request.OrderId,
            cancellationToken);

        if (roleKey is null)
            return Error.Forbidden("Order.NotParticipant", "You are not a participant of this order.");

        var respondentUserId = roleKey == DisputeEligibilityRule.RoleBuyer
            ? order.SellerId.Value
            : order.BuyerId.Value;

        var snapshot = DisputeContextSnapshotBuilder.ForOrder(order);

        return await intakeService.CreateDisputeAsync(new CreateDisputeRequest(
            Domain: request.Domain,
            CaseType: request.CaseType,
            PrimaryTargetType: DisputeEligibilityRule.TargetOrder,
            RoleKey: roleKey,
            OrderId: request.OrderId,
            AuctionId: order.AuctionId.Value,
            ShipmentId: null,
            WarehouseItemId: null,
            PaymentId: null,
            ComplainantUserId: userId.Value,
            RespondentUserId: respondentUserId,
            Title: request.Title,
            Description: request.Description,
            ContextSnapshotJson: snapshot), cancellationToken);
    }
}
