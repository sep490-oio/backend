using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.AddWarehouseReturnEvidence;

/// <summary>
/// Attaches an evidence photo to a <see cref="WarehouseToSellerShipment"/>.
/// Category is either <see cref="WarehouseReturnEvidenceCategory.PickupByWarehouseStaff"/>
/// (warehouse staff only) or <see cref="WarehouseReturnEvidenceCategory.ReceiptBySeller"/>
/// (seller only). Caller auth is enforced in the handler based on category.
/// </summary>
public sealed record AddWarehouseReturnEvidenceCommand(
    Guid ShipmentId,
    Guid MediaUploadId,
    string Category) : ICommand<WarehouseToSellerShipmentEvidenceDto>, IHasValidate
{
    public ViolationsError Validate() =>
        AddWarehouseReturnEvidenceCommand.Check()
            .WithOwnerName("AddWarehouseReturnEvidence")
            .Field(ShipmentId).NotEmptyGuid()
            .Field(MediaUploadId).NotEmptyGuid()
            .Field(Category).NotWhiteSpace();
}

internal sealed class AddWarehouseReturnEvidenceCommandHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock)
    : ICommandHandler<AddWarehouseReturnEvidenceCommand, WarehouseToSellerShipmentEvidenceDto>
{
    public async Task<Result<WarehouseToSellerShipmentEvidenceDto, Error>> Handle(
        AddWarehouseReturnEvidenceCommand request,
        CancellationToken cancellationToken)
    {
        // Resolve category from id string.
        WarehouseReturnEvidenceCategory category;
        if (request.Category == WarehouseReturnEvidenceCategory.PickupByWarehouseStaff.Id)
            category = WarehouseReturnEvidenceCategory.PickupByWarehouseStaff;
        else if (request.Category == WarehouseReturnEvidenceCategory.ReceiptBySeller.Id)
            category = WarehouseReturnEvidenceCategory.ReceiptBySeller;
        else
            return Error.Validation(
                "category",
                "WarehouseToSellerShipment.UnknownEvidenceCategory",
                $"Unknown evidence category '{request.Category}'. Expected 'pickup_by_warehouse_staff' or 'receipt_by_seller'.");

        var shipmentId = WarehouseToSellerShipmentId.From(request.ShipmentId);
        var shipment = await dbContext.Set<WarehouseToSellerShipment>()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.WarehouseToSellerShipment.NotFound(request.ShipmentId.ToString());

        // Auth: category determines actor. ReceiptBySeller -> seller only.
        // PickupByWarehouseStaff requires the BookOutbound permission which
        // is enforced at the endpoint level (warehouse-staff route). Here we
        // enforce the seller-only rule for ReceiptBySeller.
        if (category == WarehouseReturnEvidenceCategory.ReceiptBySeller)
        {
            if (shipment.SellerId != currentUser.UserId)
                return Error.Forbidden(
                    "WarehouseToSellerShipment.EvidenceForbidden",
                    "Only the seller can add receipt evidence.");
        }
        // PickupByWarehouseStaff: caller must be warehouse staff — enforced by
        // endpoint policy (App.Permissions.Catalogs.Warehouse.BookOutbound).

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

        var addResult = shipment.AddEvidence(
            nowUtc:    clock.UtcNow,
            category:  category,
            upload:    upload,
            createdBy: currentUser.UserId);

        if (addResult.IsFailure)
            return addResult.Error;

        var linkResult = upload.LinkToEntity(addResult.Value.Id, clock.UtcNow);
        if (linkResult.IsFailure)
            return linkResult.Error;

        dbContext.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var evidence = addResult.Value;
        return new WarehouseToSellerShipmentEvidenceDto(
            Id:            evidence.Id.Value,
            ShipmentId:    evidence.ShipmentId.Value,
            Category:      evidence.Category,
            MediaUploadId: evidence.MediaUploadId.Value,
            SecureUrl:     evidence.SecureUrl,
            FileName:      evidence.FileName,
            ResourceType:  evidence.ResourceType,
            CreatedAt:     evidence.CreatedAt,
            CreatedBy:     evidence.CreatedBy.Value);
    }
}
