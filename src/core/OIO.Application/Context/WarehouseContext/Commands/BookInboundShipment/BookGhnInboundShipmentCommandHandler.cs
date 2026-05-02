using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Errors;
using Item = OIO.Domain.Context.CatalogContext.Aggregates.Items.Item;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

internal sealed class BookGhnInboundShipmentCommandHandler
    : ICommandHandler<BookGhnInboundShipmentCommand, List<InboundShipmentDto>>
{
    private readonly IDbContext       _dbContext;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ICurrentUser     _currentUser;
    private readonly IShippingService _shippingService;
    private readonly IClock           _clock;

    public BookGhnInboundShipmentCommandHandler(
        IDbContext       dbContext,
        IUnitOfWork      unitOfWork,
        ICurrentUser     currentUser,
        IShippingService shippingService,
        IClock           clock)
    {
        _dbContext       = dbContext;
        _unitOfWork      = unitOfWork;
        _currentUser     = currentUser;
        _shippingService = shippingService;
        _clock           = clock;
    }

    public async Task<Result<List<InboundShipmentDto>, Error>> Handle(
        BookGhnInboundShipmentCommand request,
        CancellationToken          cancellationToken)
    {
        var now        = _clock.UtcNow;
        var totalWeight = request.WeightGrams;

        // ── 1. Validate & Build package dimensions ───────────────────────────
        if (totalWeight <= 0)
            return Error.Validation("WeightGrams", "BookGhnInbound.WeightRequired", "Total weight must be > 0.");
            
        if (request.LengthCm <= 0 || request.WidthCm <= 0 || request.HeightCm <= 0)
            return Error.Validation("Dimensions", "BookGhnInbound.InvalidDimensions", "Dimensions must be > 0.");

        var dimensionsResult = PackageDimensions.Create(
            weightGrams: totalWeight,
            lengthCm:    request.LengthCm ?? 10,
            widthCm:     request.WidthCm  ?? 10,
            heightCm:    request.HeightCm ?? 10);

        if (dimensionsResult.IsFailure) return dimensionsResult.Error;
        var dimensions = dimensionsResult.Value;

        // ── 2. Eligibility checks (simplified copy from main handler) ────────
        var distinctItemIds = request.Items.Select(i => i.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != request.Items.Count)
            return Error.Validation("Items", "BookGhnInbound.DuplicateItemId", "Duplicate ItemId values.");

        // Check DB for existing shipments and eligibility
        var isAdmin        = _currentUser.IsInRole(App.Roles.Catalogs.Admin);
        var rawItemIds = request.Items.Select(i => i.ItemId).ToList();
        var requestItemIds = rawItemIds.Select(ItemId.From).ToList();
        
        var query = _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => requestItemIds.Contains(i.Id));

        if (!isAdmin)
        {
            query = query.Where(i => i.SellerId == _currentUser.UserId);
        }

        var eligibilityRows = await query
            .Select(i => new
            {
                ItemId = i.Id.Value,
                StatusId = i.Status.Id,
                RequiresPlatformInspection = i.RequiresPlatformInspection,
            })
            .ToListAsync(cancellationToken);

        var eligibilityMap = eligibilityRows.ToDictionary(r => r.ItemId);

        // 1. Check for existing active shipments (Single query for all items)
        var activeShipmentItemId = await _dbContext.Set<InboundShipment>()
            .Where(s => rawItemIds.Contains(s.ItemId) &&
                        s.Status != InboundShipmentStatus.Cancelled &&
                        s.Status != InboundShipmentStatus.Failed)
            .Select(s => s.ItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeShipmentItemId != Guid.Empty)
            return WarehouseErrors.InboundShipment.AlreadyExists(activeShipmentItemId.ToString());

        foreach (var item in request.Items)
        {
            // 2. Check eligibility
            if (!eligibilityMap.TryGetValue(item.ItemId, out var row))
            {
                return Error.Validation(
                    "Items",
                    "BookGhnInbound.ItemNotFoundOrNotOwned",
                    $"Item {item.ItemId} does not exist or does not belong to the current seller.");
            }

            if (!row.RequiresPlatformInspection)
            {
                return Error.Validation(
                    "Items",
                    "BookGhnInbound.ItemDoesNotRequirePlatformInspection",
                    $"Item {item.ItemId} is not flagged for platform verification.");
            }

            if (!string.Equals(row.StatusId, "pending_verify", StringComparison.OrdinalIgnoreCase))
            {
                return Error.Validation(
                    "Items",
                    "BookGhnInbound.ItemNotPendingVerify",
                    $"Item {item.ItemId} must be in status 'pending_verify' (current: {row.StatusId}).");
            }
        }

        // ── 3. Resolve Sender Address ────────────────────────────────────────
        var senderName     = request.SenderName;
        var senderPhone    = request.SenderPhone;
        var senderAddress  = request.SenderAddress;
        var senderWard     = request.SenderWard;
        var senderDistrict = request.SenderDistrict;
        var senderProvince = request.SenderProvince;

        if (string.IsNullOrWhiteSpace(senderName)    ||
            string.IsNullOrWhiteSpace(senderPhone)   ||
            string.IsNullOrWhiteSpace(senderAddress) ||
            string.IsNullOrWhiteSpace(senderWard)    ||
            string.IsNullOrWhiteSpace(senderDistrict)||
            string.IsNullOrWhiteSpace(senderProvince))
        {
            var defaultUserAddress = await _dbContext.Set<UserAddress>()
                .FirstOrDefaultAsync(a => a.UserId == _currentUser.UserId && a.IsDefault, cancellationToken);

            if (defaultUserAddress is null)
                return WarehouseErrors.InboundShipment.SenderAddressMissingAndNoDefault;

            senderName     = string.IsNullOrWhiteSpace(senderName)     ? defaultUserAddress.Recipient.RecipientName : senderName;
            senderPhone    = string.IsNullOrWhiteSpace(senderPhone)    ? defaultUserAddress.Recipient.Phone.Value    : senderPhone;
            senderAddress  = string.IsNullOrWhiteSpace(senderAddress)  ? defaultUserAddress.Address.Street          : senderAddress;
            senderWard     = string.IsNullOrWhiteSpace(senderWard)     ? defaultUserAddress.Address.Ward            : senderWard;
            senderDistrict = string.IsNullOrWhiteSpace(senderDistrict) ? defaultUserAddress.Address.District        : senderDistrict;
            senderProvince = string.IsNullOrWhiteSpace(senderProvince) ? defaultUserAddress.Address.City            : senderProvince;
        }

        // ── 4. Load GHN Config ───────────────────────────────────────────────
        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == ShippingProviderCode.Ghn && c.IsActive, cancellationToken);

        if (config is null)
            return WarehouseErrors.ShippingProvider.NotFound("ghn");

        // ── 5. Prepare Metadata in ExtraDataJson ─────────────────────────────
        var metadataJson = JsonSerializer.Serialize(new { ghn_sender_metadata = request.SenderMetadata });

        // ── 6. Call carrier API ──────────────────────────────────────────────
        var clientOrderCode = $"INB-GHN-{Guid.NewGuid():N}"[..20];
        
        var catalogItemIds = request.Items.Select(x => ItemId.From(x.ItemId)).ToList();
        var itemTitlesDict = await _dbContext.Set<Item>()
            .Where(i => catalogItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id.Value, i => i.Title.Value, cancellationToken);
            
        var fallbackPrice = Math.Max(0, Math.Round(request.InsuranceValue / request.Items.Count));

        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = clientOrderCode,
            RecipientName   = config.PickName,
            RecipientPhone  = config.PickPhone,
            RecipientAddress = config.PickAddress,
            RecipientWard    = config.PickWard,
            RecipientDistrict = config.PickDistrict,
            RecipientProvince = config.PickProvince,
            RecipientCarrierAddressDataJson = config.PickCarrierAddressData?.RawJson,

            SenderName     = senderName,
            SenderPhone    = senderPhone,
            SenderAddress  = senderAddress,
            SenderWard     = senderWard,
            SenderDistrict = senderDistrict,
            SenderProvince = senderProvince,
            SenderCarrierAddressDataJson = request.SenderCarrierAddressDataJson,

            WeightGrams    = totalWeight,
            LengthCm       = request.LengthCm ?? 10,
            WidthCm        = request.WidthCm  ?? 10,
            HeightCm       = request.HeightCm ?? 10,
            InsuranceValue = request.InsuranceValue,
            CodAmount      = 0,

            GhnHandlingNote = request.GhnHandlingNote,
            ExtraDataJson   = metadataJson, // <--- Injection point for metadata

            Items = BuildCarrierItemList(request, itemTitlesDict, totalWeight, fallbackPrice, clientOrderCode)
        };

        var bookingResult = await _shippingService.BookShipmentAsync(
            "ghn", bookingRequest, config, cancellationToken);

        if (bookingResult.IsFailure) return bookingResult.Error;
        var booking = bookingResult.Value;

        // ── 7. Create shipments & persist ────────────────────────────────────
        var senderCarrierData = request.SenderCarrierAddressDataJson is not null
            ? CarrierAddressData.From(request.SenderCarrierAddressDataJson)
            : null;

        var createdShipments = new List<InboundShipment>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var createResult = InboundShipment.Create(
                itemId:          item.ItemId,
                sellerId:        _currentUser.UserId,
                providerCode:    ShippingProviderCode.Ghn,
                clientOrderCode: clientOrderCode,
                senderName:      senderName,
                senderPhone:     senderPhone,
                senderAddress:   senderAddress,
                senderWard:      senderWard,
                senderDistrict:  senderDistrict,
                senderProvince:  senderProvince,
                dimensions:      dimensions,
                now:             now,
                shipmentMode:    InboundShipmentMode.PlatformManaged,
                senderCarrierAddressData: senderCarrierData,
                shippingFee:       booking.ShippingFee,
                insuranceValue:    request.InsuranceValue,
                notes:             request.Notes,
                expectedArrivalAt: booking.EstimatedDeliveryAt);

            if (createResult.IsFailure) return createResult.Error;

            var shipment = createResult.Value;
            shipment.RecordBooked(booking.CarrierTrackingNumber, now);

            createdShipments.Add(shipment);
            _dbContext.Insert(shipment);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return createdShipments.Select(s => s.ToDto()).ToList();
    }

    private static List<BookShipmentItem> BuildCarrierItemList(
        BookGhnInboundShipmentCommand request,
        Dictionary<Guid, string> itemTitlesDict,
        int totalWeight,
        decimal fallbackPricePerItem,
        string clientOrderCode)
    {
        var count = request.Items.Count;
        var baseWeight = totalWeight / count;
        var remainder = totalWeight - (baseWeight * count);
        var list = new List<BookShipmentItem>(count);
        for (int idx = 0; idx < count; idx++)
        {
            var i = request.Items[idx];
            var weight = baseWeight + (idx < remainder ? 1 : 0);
            list.Add(new BookShipmentItem
            {
                Name        = itemTitlesDict.GetValueOrDefault(i.ItemId, "Unknown Item"),
                Code        = clientOrderCode,
                Quantity    = 1,
                Price       = i.ItemPrice ?? fallbackPricePerItem,
                WeightGrams = weight,
            });
        }
        return list;
    }
}
