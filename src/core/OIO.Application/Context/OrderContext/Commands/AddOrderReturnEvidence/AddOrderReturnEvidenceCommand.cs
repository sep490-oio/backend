using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.Errors;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Commands.AddOrderReturnEvidence;

/// <summary>
/// Attaches an evidence photo to an <see cref="OrderReturn"/>. Category is
/// either <see cref="OrderReturnEvidenceCategory.PickupByBuyer"/> (buyer-only)
/// or <see cref="OrderReturnEvidenceCategory.ReceiptBySeller"/> (seller-only).
/// Caller auth is enforced in the handler based on category.
/// </summary>
public sealed record AddOrderReturnEvidenceCommand(
    Guid OrderId,
    Guid ReturnId,
    Guid MediaUploadId,
    string Category) : ICommand<OrderReturnEvidenceDto>, IHasValidate
{
    public ViolationsError Validate() =>
        AddOrderReturnEvidenceCommand.Check()
            .WithOwnerName("AddOrderReturnEvidence")
            .Field(OrderId).NotEmptyGuid()
            .Field(ReturnId).NotEmptyGuid()
            .Field(MediaUploadId).NotEmptyGuid()
            .Field(Category).NotWhiteSpace();
}

internal sealed class AddOrderReturnEvidenceCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddOrderReturnEvidenceCommand, OrderReturnEvidenceDto>
{
    public async Task<Result<OrderReturnEvidenceDto, Error>> Handle(
        AddOrderReturnEvidenceCommand request,
        CancellationToken cancellationToken)
    {
        // Resolve category from id string.
        OrderReturnEvidenceCategory category;
        if (request.Category == OrderReturnEvidenceCategory.PickupByBuyer.Id)
            category = OrderReturnEvidenceCategory.PickupByBuyer;
        else if (request.Category == OrderReturnEvidenceCategory.ReceiptBySeller.Id)
            category = OrderReturnEvidenceCategory.ReceiptBySeller;
        else
            return Error.Validation(
                "category",
                "OrderReturn.UnknownEvidenceCategory",
                $"Unknown evidence category '{request.Category}'. Expected 'pickup_by_buyer' or 'receipt_by_seller'.");

        var order = await dbContext.Set<Order>()
            .Include(x => x.Return)
                .ThenInclude(r => r!.Evidence)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return OrderErrors.Order.NotFound(OrderId.From(request.OrderId));

        if (order.Return is null || order.Return.Id != OrderReturnId.From(request.ReturnId))
            return OrderErrors.Order.ReturnNotFound(order.Id);

        // Auth: category determines actor. Buyer = PickupByBuyer; Seller = ReceiptBySeller.
        if (category == OrderReturnEvidenceCategory.PickupByBuyer)
        {
            if (order.BuyerId != currentUser.UserId)
                return Error.Forbidden(
                    "OrderReturn.EvidenceForbidden",
                    "Only the buyer can add pickup evidence.");
        }
        else // ReceiptBySeller
        {
            if (order.SellerId != currentUser.UserId)
                return Error.Forbidden(
                    "OrderReturn.EvidenceForbidden",
                    "Only the seller can add receipt evidence.");
        }

        var upload = await dbContext.Set<MediaUpload>()
            .FirstOrDefaultAsync(m => m.Id == MediaUploadId.From(request.MediaUploadId), cancellationToken);

        if (upload is null)
            return Error.NotFound(
                "Media.NotFound",
                $"MediaUpload with id {request.MediaUploadId} not found.");

        if (!upload.IsConfirmed)
            return Error.Conflict(
                "Media.NotConfirmed",
                "MediaUpload must be confirmed before it can be attached as evidence.");

        var addResult = order.Return.AddEvidence(
            nowUtc:    clock.UtcNow,
            category:  category,
            upload:    upload,
            createdBy: currentUser.UserId);

        if (addResult.IsFailure)
            return addResult.Error;

        // Link the media to the evidence row so the linker can treat the
        // evidence as its owner (mirrors OutboundShipmentEvidence pattern).
        var linkResult = upload.LinkToEntity(addResult.Value.Id, clock.UtcNow);
        if (linkResult.IsFailure)
            return linkResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var evidence = addResult.Value;
        return new OrderReturnEvidenceDto(
            Id:            evidence.Id.Value,
            OrderReturnId: evidence.OrderReturnId.Value,
            Category:      evidence.Category,
            MediaUpload:   new OrderReturnEvidenceMediaDto(
                Id:           evidence.MediaUploadId.Value,
                SecureUrl:    evidence.SecureUrl,
                FileName:     evidence.FileName,
                ResourceType: evidence.ResourceType),
            CreatedAt:     evidence.CreatedAt,
            CreatedBy:     evidence.CreatedBy.Value);
    }
}
