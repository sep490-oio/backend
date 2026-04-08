using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.UpdateOrderShipping;

/// <summary>
/// Buyer updates the shipping snapshot on a pending-payment order.
/// Runs before the actual payment attempt so the delivery address is
/// locked in before funds move.
///
/// Authorization: only the order's buyer may update.
/// Preconditions: order must be in <c>pending_payment</c> status.
/// Validation: all address-book required fields must be present
/// (recipient, phone, street, ward, district, city). Postal code optional.
/// </summary>
public sealed record UpdateOrderShippingCommand(
    Guid OrderId,
    string RecipientName,
    string PhoneNumber,
    string Street,
    string Ward,
    string District,
    string City,
    string? PostalCode) : ICommand<OrderDto>, IHasValidate
{
    public ViolationsError Validate() =>
        UpdateOrderShippingCommand.Check()
            .WithOwnerName("UpdateOrderShipping")
            .Field(OrderId).NotEmptyGuid()
            .Field(RecipientName).NotWhiteSpace()
            .Field(PhoneNumber).NotWhiteSpace()
            .Field(Street).NotWhiteSpace()
            .Field(Ward).NotWhiteSpace()
            .Field(District).NotWhiteSpace()
            .Field(City).NotWhiteSpace();
}

internal sealed class UpdateOrderShippingCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<UpdateOrderShippingCommand, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        UpdateOrderShippingCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(request.OrderId);

        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(x => x.Id == orderId, cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(orderId);

        // Only the order's buyer may update the shipping snapshot.
        if (order.BuyerId != currentUser.UserId)
            return Error.Forbidden("Order.UpdateShippingForbidden", "Only the order buyer can update shipping information.");

        // Build a structured snapshot; the factory enforces address-book rules.
        var snapshotResult = ShippingSnapshot.CreateStructured(
            recipientName: request.RecipientName,
            phone: request.PhoneNumber,
            street: request.Street,
            ward: request.Ward,
            district: request.District,
            city: request.City,
            postalCode: request.PostalCode);

        if (snapshotResult.IsFailure)
            return snapshotResult.Error;

        var updateResult = order.UpdateShipping(snapshotResult.Value, clock.UtcNow);
        if (updateResult.IsFailure)
            return updateResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
