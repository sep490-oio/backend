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
using OIO.Domain.SeedWork.Errors;
using Item = OIO.Domain.Context.CatalogContext.Aggregates.Items.Item;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

internal sealed class BookInboundShipmentCommandHandler
    : ICommandHandler<BookInboundShipmentCommand, List<InboundShipmentDto>>
{
    private readonly IDbContext       _dbContext;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ICurrentUser     _currentUser;
    private readonly IShippingService _shippingService;
    private readonly IClock           _clock;

    public BookInboundShipmentCommandHandler(
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
        BookInboundShipmentCommand request,
        CancellationToken          cancellationToken)
    {
        var now        = _clock.UtcNow;
        var isExternal = request.ShipmentMode == InboundShipmentMode.ExternalCarrier.Id;

        // ── 1. Validate & Build package dimensions ───────────────────────────
        // The inbound batch flow uses request.WeightGrams as the single source of
        // truth for the whole parcel. Item-level WeightGrams from the request are
        // intentionally ignored (older clients may still send them but they must
        // not influence carrier pricing — we derive item weights from the total).
        var totalWeight = request.WeightGrams;
        if (totalWeight <= 0)
        {
            return Error.Validation(
                "WeightGrams",
                "BookInboundShipment.WeightRequired",
                "Total package weight must be greater than 0.");
        }
        if (totalWeight < request.Items.Count)
        {
            return Error.Validation(
                "WeightGrams",
                "BookInboundShipment.WeightTooLow",
                $"Total package weight ({totalWeight}g) must be at least 1g per selected item ({request.Items.Count} items).");
        }

        if (!isExternal && (request.LengthCm <= 0 || request.WidthCm <= 0 || request.HeightCm <= 0))
        {
            return Error.Validation(
                "Dimensions",
                "BookInboundShipment.InvalidDimensions",
                "Length, width, and height must be greater than 0 for platform-managed shipments.");
        }

        var dimensionsResult = PackageDimensions.Create(
            weightGrams: totalWeight,
            lengthCm:    request.LengthCm ?? 10, // Provide defaults if missing but validated above
            widthCm:     request.WidthCm  ?? 10,
            heightCm:    request.HeightCm ?? 10);

        if (dimensionsResult.IsFailure) return dimensionsResult.Error;
        var dimensions = dimensionsResult.Value;

        // ── 2. Duplicate check — each ItemId must not have an active inbound ─────
        // First: check for duplicates within the request itself (prevents same ItemId appearing twice)
        var distinctItemIds = request.Items.Select(i => i.ItemId).Distinct().ToList();
        if (distinctItemIds.Count != request.Items.Count)
            return Error.Validation(
                "Items",
                "BookInboundShipment.DuplicateItemId",
                "The Items list contains duplicate ItemId values. Each item must appear only once.");

        var rawItemIds = request.Items.Select(i => i.ItemId).ToList();
        var requestItemIds = rawItemIds.Select(ItemId.From).ToList();

        // Second: check DB — each ItemId must not already have an active inbound shipment
        var activeShipmentItemId = await _dbContext.Set<InboundShipment>()
            .Where(s => rawItemIds.Contains(s.ItemId) &&
                        s.Status.Id != InboundShipmentStatus.Cancelled.Id &&
                        s.Status.Id != InboundShipmentStatus.Failed.Id)
            .Select(s => s.ItemId)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeShipmentItemId != Guid.Empty)
            return WarehouseErrors.InboundShipment.AlreadyExists(activeShipmentItemId.ToString());

        // Third: verify each item is actually eligible for platform verification.
        //   - item must belong to the current seller
        //   - item.Status must be pending_verify
        //   - item.RequiresPlatformInspection must be true (canonical flag)
        // This stops manual/misrouted requests from booking inbound for items that
        // were never routed through the platform verification workflow.
        var eligibilityRows = await _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => requestItemIds.Contains(i.Id) && i.SellerId == _currentUser.UserId)
            .Select(i => new
            {
                ItemId = i.Id.Value,
                StatusId = i.Status.Id,
                RequiresPlatformInspection = i.RequiresPlatformInspection,
            })
            .ToListAsync(cancellationToken);

        var eligibilityMap = eligibilityRows.ToDictionary(r => r.ItemId);
        foreach (var item in request.Items)
        {
            if (!eligibilityMap.TryGetValue(item.ItemId, out var row))
            {
                return Error.Validation(
                    "Items",
                    "BookInboundShipment.ItemNotFoundOrNotOwned",
                    $"Item {item.ItemId} does not exist or does not belong to the current seller.");
            }

            if (!row.RequiresPlatformInspection)
            {
                return Error.Validation(
                    "Items",
                    "BookInboundShipment.ItemDoesNotRequirePlatformInspection",
                    $"Item {item.ItemId} is not flagged for platform verification. Only items submitted with platform verification may be booked for inbound.");
            }

            if (!string.Equals(row.StatusId, "pending_verify", StringComparison.OrdinalIgnoreCase))
            {
                return Error.Validation(
                    "Items",
                    "BookInboundShipment.ItemNotPendingVerify",
                    $"Item {item.ItemId} must be in status 'pending_verify' to book inbound (current: {row.StatusId}).");
            }
        }

        // ── 3. Resolve Sender Address ─────────────────────────────────────────
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

        // ── 4a. External carrier — one record per item, shared ClientOrderCode ──
        if (isExternal)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalCarrierName))
                return WarehouseErrors.InboundShipment.ExternalCarrierNameRequired;

            var externalShipments = new List<InboundShipment>(request.Items.Count);
            var sharedExtCode = $"EXT-{Guid.NewGuid():N}"[..20];

            foreach (var item in request.Items)
            {

                var extResult = InboundShipment.Create(
                    itemId:              item.ItemId,
                    sellerId:            _currentUser.UserId,
                    providerCode:        ShippingProviderCode.External,
                    clientOrderCode:     sharedExtCode,
                    senderName:          senderName,
                    senderPhone:         senderPhone,
                    senderAddress:       senderAddress,
                    senderWard:          senderWard,
                    senderDistrict:      senderDistrict,
                    senderProvince:      senderProvince,
                    dimensions:          dimensions,
                    now:                 now,
                    shipmentMode:        InboundShipmentMode.ExternalCarrier,
                    externalCarrierName: request.ExternalCarrierName,
                    insuranceValue:      request.InsuranceValue,
                    notes:               request.Notes);

                if (extResult.IsFailure) return extResult.Error;

                externalShipments.Add(extResult.Value);
                _dbContext.Insert(extResult.Value);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return externalShipments.Select(s => s.ToDto()).ToList();
        }

        // ── 4b. Platform-managed — load ShippingProviderConfig ────────────────
        ShippingProviderConfig? config;

        if (!string.IsNullOrWhiteSpace(request.ProviderCode))
        {
            var providerCode = ShippingProviderCode.FromId(request.ProviderCode);
            if (providerCode.HasNoValue)
                return WarehouseErrors.ShippingProvider.NotFound(request.ProviderCode);

            config = await _dbContext.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(
                    c => c.ProviderCode == providerCode.Value && c.IsActive,
                    cancellationToken);

            if (config is null)
                return WarehouseErrors.ShippingProvider.NotFound(request.ProviderCode);
        }
        else
        {
            config = await _dbContext.Set<ShippingProviderConfig>()
                .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive, cancellationToken);

            if (config is null)
                return WarehouseErrors.ShippingProvider.NoDefaultProvider;
        }

        // ── 5. Generate ONE shared client order code for the batch ────────────
        var clientOrderCode = $"INB-{Guid.NewGuid():N}"[..20];

        // Fetch item titles from catalog to use as Name in the carrier order
        var catalogItemIds = request.Items.Select(x => ItemId.From(x.ItemId)).ToList();
        var itemTitlesDict = await _dbContext.Set<Item>()
            .Where(i => catalogItemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id.Value, i => i.Title.Value, cancellationToken);
            
        // Fallback calculation if ItemPrice is omitting: distribute InsuranceValue evenly
        var fallbackPricePerItem = Math.Max(0, Math.Round(request.InsuranceValue / request.Items.Count));

        // ── 6. Call carrier API ONCE with all items listed in Items[] ─────────
        // Recipient = warehouse (pick address from config)
        // Sender    = seller's address (carrier picks up FROM seller)
        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = clientOrderCode,

            // Warehouse is the delivery destination
            RecipientName                   = config.PickName,
            RecipientPhone                  = config.PickPhone,
            RecipientAddress                = config.PickAddress,
            RecipientWard                   = config.PickWard,
            RecipientDistrict               = config.PickDistrict,
            RecipientProvince               = config.PickProvince,
            RecipientCarrierAddressDataJson  = config.PickCarrierAddressData?.RawJson,

            // Seller is the pickup point
            SenderName                   = senderName,
            SenderPhone                  = senderPhone,
            SenderAddress                = senderAddress,
            SenderWard                   = senderWard,
            SenderDistrict               = senderDistrict,
            SenderProvince               = senderProvince,
            SenderCarrierAddressDataJson  = request.SenderCarrierAddressDataJson,

            WeightGrams    = totalWeight,
            LengthCm       = request.LengthCm ?? 10,
            WidthCm        = request.WidthCm  ?? 10,
            HeightCm       = request.HeightCm ?? 10,
            InsuranceValue = request.InsuranceValue,
            CodAmount      = 0,   // inbound — no COD

            GhnHandlingNote = request.GhnHandlingNote,

            // All items declared in a single GHN order.
            // Item-level weights are derived from the top-level total weight using
            // an even distribution: base = totalWeight / itemCount, remainder is
            // added to the first `remainder` items so the sum == totalWeight.
            Items = BuildCarrierItemList(request, itemTitlesDict, totalWeight, fallbackPricePerItem, clientOrderCode)
        };

        var bookingResult = await _shippingService.BookShipmentAsync(
            config.ProviderCode.Id, bookingRequest, config, cancellationToken);

        if (bookingResult.IsFailure) return bookingResult.Error;
        var booking = bookingResult.Value;

        // ── 7. Create ONE InboundShipment per ItemId, all sharing the GHN order ─
        // ClientOrderCode and CarrierTrackingNumber are identical across the batch.
        // Staff still processes each shipment independently by its own InboundShipmentId.
        var senderCarrierData = request.SenderCarrierAddressDataJson is not null
            ? CarrierAddressData.From(request.SenderCarrierAddressDataJson)
            : null;

        var createdShipments = new List<InboundShipment>(request.Items.Count);

        foreach (var item in request.Items)
        {
            var createResult = InboundShipment.Create(
                itemId:          item.ItemId,
                sellerId:        _currentUser.UserId,
                providerCode:    config.ProviderCode,
                clientOrderCode: clientOrderCode,          // ← shared across batch
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
                shippingFee:       booking.ShippingFee,    // ← total fee, denormalized per record
                insuranceValue:    request.InsuranceValue,
                notes:             request.Notes,
                expectedArrivalAt: booking.EstimatedDeliveryAt);

            if (createResult.IsFailure) return createResult.Error;

            var shipment = createResult.Value;

            // Sets shared CarrierTrackingNumber on each record
            var bookedResult = shipment.RecordBooked(booking.CarrierTrackingNumber, now);
            if (bookedResult.IsFailure) return bookedResult.Error;

            createdShipments.Add(shipment);
            _dbContext.Insert(shipment);
        }

        // ── 8. Persist all at once ────────────────────────────────────────────
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return createdShipments.Select(s => s.ToDto()).ToList();
    }

    /// <summary>
    /// Distributes the top-level package weight evenly across the requested items
    /// for carrier metadata. Base = total / count, remainder is added to the first
    /// <c>remainder</c> items. The resulting sum is exactly <paramref name="totalWeight"/>.
    /// Item-level WeightGrams from the incoming request is intentionally ignored.
    /// </summary>
    private static List<BookShipmentItem> BuildCarrierItemList(
        BookInboundShipmentCommand request,
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