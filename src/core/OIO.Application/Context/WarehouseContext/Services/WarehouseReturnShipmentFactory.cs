using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Services;

/// <summary>
/// Factory for ensuring a <c>WarehouseToSellerShipment</c> exists for a rejected
/// inspection. Used by both the domain-event handler (normal flow) and the
/// admin <c>RetryPendingInspectionRejectCommand</c> (recovery flow).
///
/// Missing-address handling follows plan H3:
///   - Normal flow (via event handler): the handler catches the thrown
///     <see cref="InvalidOperationException"/> and lets the outbox retry.
///   - Recovery flow (via retry command): the command maps the thrown exception
///     to a validation <see cref="Error"/> the caller surfaces as 4xx.
/// </summary>
internal interface IWarehouseReturnShipmentFactory
{
    Task<EnsureShipmentResult> EnsureShipmentExistsAsync(
        WarehouseInspectionId inspectionId,
        WarehouseItemId warehouseItemId,
        string rejectionReason,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        bool persistImmediately = true);
}

internal enum EnsureShipmentOutcome
{
    Created,
    AlreadyExists,
    CreatedViaDbDedup,
    WarehouseItemNotFound,
    SellerNotResolved,
    SellerAddressMissing,
    CreateFailed,
    TransitionFailed,
}

internal sealed record EnsureShipmentResult(
    EnsureShipmentOutcome Outcome,
    WarehouseToSellerShipment? Shipment = null,
    Error? Error = null);

internal sealed class WarehouseReturnShipmentFactory(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ILogger<WarehouseReturnShipmentFactory> logger)
    : IWarehouseReturnShipmentFactory
{
    public async Task<EnsureShipmentResult> EnsureShipmentExistsAsync(
        WarehouseInspectionId inspectionId,
        WarehouseItemId warehouseItemId,
        string rejectionReason,
        DateTime nowUtc,
        CancellationToken cancellationToken,
        bool persistImmediately = true)
    {
        // 1) Idempotency query-first-check.
        var existingId = await dbContext.Set<WarehouseToSellerShipment>()
            .AsNoTracking()
            .Where(s => s.WarehouseInspectionId == inspectionId
                     && s.Status != WarehouseToSellerShipmentStatus.Closed
                     && s.Status != WarehouseToSellerShipmentStatus.ReturnedToWarehouse)
            .Select(s => (Guid?)s.Id.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingId.HasValue)
        {
            logger.LogInformation(
                "WarehouseToSellerShipment already exists for inspection {InspectionId} (shipment {ShipmentId}) — skipping.",
                inspectionId.Value,
                existingId.Value);
            return new EnsureShipmentResult(EnsureShipmentOutcome.AlreadyExists);
        }

        // 2) Load the warehouse item so we can transition its status.
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(w => w.Id == warehouseItemId, cancellationToken);

        if (warehouseItem is null)
        {
            logger.LogWarning(
                "WarehouseItem {WarehouseItemId} not found for inspection-reject shipment (inspection {InspectionId}).",
                warehouseItemId.Value,
                inspectionId.Value);
            return new EnsureShipmentResult(
                EnsureShipmentOutcome.WarehouseItemNotFound,
                Error: Error.NotFound(
                    "WarehouseItem.NotFound",
                    $"Warehouse item '{warehouseItemId.Value}' was not found."));
        }

        // 3) Derive the seller: WarehouseItem → Item → SellerId.
        var itemId = ItemId.From(warehouseItem.ItemId);
        var sellerGuid = await dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => (Guid?)i.SellerId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (!sellerGuid.HasValue || sellerGuid.Value == Guid.Empty)
        {
            logger.LogError(
                "Stuck inspection-reject: could not resolve SellerId for WarehouseItem {WarehouseItemId} / Item {ItemId}. Inspection {InspectionId}.",
                warehouseItemId.Value,
                warehouseItem.ItemId,
                inspectionId.Value);
            return new EnsureShipmentResult(
                EnsureShipmentOutcome.SellerNotResolved,
                Error: Error.Validation(
                    "SellerId",
                    "WarehouseToSellerShipment.SellerNotResolved",
                    $"Could not resolve SellerId for WarehouseItem {warehouseItemId.Value}."));
        }

        var sellerId = UserId.From(sellerGuid.Value);

        // 4) Load the seller's default UserAddress.
        var address = await dbContext.Set<UserAddress>()
            .AsNoTracking()
            .Where(a => a.UserId == sellerId && a.IsDefault)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (address is null)
        {
            var nonDefaultCount = await dbContext.Set<UserAddress>()
                .AsNoTracking()
                .CountAsync(a => a.UserId == sellerId && !a.IsDefault, cancellationToken);

            logger.LogError(
                "Stuck inspection-reject: Seller {SellerId} has no default UserAddress " +
                "(has {NonDefaultCount} non-default). Inspection {InspectionId}, WarehouseItem {WarehouseItemId}.",
                sellerId.Value,
                nonDefaultCount,
                inspectionId.Value,
                warehouseItemId.Value);

            return new EnsureShipmentResult(
                EnsureShipmentOutcome.SellerAddressMissing,
                Error: Error.Validation(
                    "SellerAddress",
                    "WarehouseToSellerShipment.SellerAddressMissing",
                    $"Seller {sellerId.Value} has no default UserAddress. Admin must add a default address and retry."));
        }

        // 5) Snapshot the address as JSON.
        var snapshot = JsonSerializer.Serialize(new
        {
            userAddressId = address.Id.Value,
            recipient = new
            {
                name  = address.Recipient.RecipientName,
                phone = address.Recipient.Phone.Value,
            },
            address = new
            {
                street     = address.Address.Street,
                ward       = address.Address.Ward,
                district   = address.Address.District,
                city       = address.Address.City,
                postalCode = address.Address.PostalCode,
            },
            type      = address.Type.Id,
            isDefault = address.IsDefault,
            snapshottedAt = nowUtc,
        });

        // 6) Flip the WarehouseItem into the return-to-seller flow.
        var transitionResult = warehouseItem.StartReturnToSeller(nowUtc);
        if (transitionResult.IsFailure)
        {
            logger.LogWarning(
                "WarehouseItem {WarehouseItemId} cannot transition to AwaitingSellerReturn. Inspection {InspectionId}. Error={Error}.",
                warehouseItemId.Value,
                inspectionId.Value,
                transitionResult.Error.Message);
            return new EnsureShipmentResult(
                EnsureShipmentOutcome.TransitionFailed,
                Error: transitionResult.Error);
        }

        // 7) Create the shipment aggregate.
        var createResult = WarehouseToSellerShipment.Create(
            warehouseItemId:       warehouseItemId,
            warehouseInspectionId: inspectionId,
            sellerId:              sellerId,
            sellerAddressSnapshot: snapshot,
            rejectionReason:       rejectionReason,
            nowUtc:                nowUtc);

        if (createResult.IsFailure)
        {
            logger.LogError(
                "Failed to create WarehouseToSellerShipment for inspection {InspectionId}. Error={Error}.",
                inspectionId.Value,
                createResult.Error.Message);
            return new EnsureShipmentResult(
                EnsureShipmentOutcome.CreateFailed,
                Error: createResult.Error);
        }

        var shipment = createResult.Value;

        // 8) Raise the side-effect event BEFORE SaveChangesAsync so the outbox
        //    captures it in the same transactional boundary as the insert.
        shipment.MarkCreated();

        dbContext.Insert(shipment);
        dbContext.Update(warehouseItem);

        if (!persistImmediately)
        {
            logger.LogInformation(
                "WarehouseToSellerShipment staged for inspection {InspectionId} (shipment {ShipmentId}, warehouseItem {WarehouseItemId}).",
                inspectionId.Value,
                shipment.Id.Value,
                warehouseItemId.Value);

            return new EnsureShipmentResult(EnsureShipmentOutcome.Created, shipment);
        }

        // 9) DB-level idempotency safety net (H2). Only SqlState 23505 is
        //    swallowed — every other DbUpdateException propagates.
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
        {
            logger.LogInformation(
                "WarehouseToSellerShipment DB-level dedup triggered for inspection {InspectionId} — treating as idempotent.",
                inspectionId.Value);
            dbContext.DetachAll();
            return new EnsureShipmentResult(EnsureShipmentOutcome.CreatedViaDbDedup);
        }

        logger.LogInformation(
            "WarehouseToSellerShipment created (shipment {ShipmentId}, warehouseItem {WarehouseItemId}, seller {SellerId}, inspection {InspectionId}).",
            shipment.Id.Value,
            warehouseItemId.Value,
            sellerId.Value,
            inspectionId.Value);

        return new EnsureShipmentResult(EnsureShipmentOutcome.Created, shipment);
    }
}
