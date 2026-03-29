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

        // ── 1. Build package dimensions (shared for the entire batch) ─────────
        var dimensionsResult = PackageDimensions.Create(
            weightGrams: request.WeightGrams,
            lengthCm:    request.LengthCm,
            widthCm:     request.WidthCm,
            heightCm:    request.HeightCm);

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

        // Second: check DB — each ItemId must not already have an active inbound shipment
        foreach (var item in request.Items)
        {
            var hasActive = await _dbContext.Set<InboundShipment>()
                .AnyAsync(s => s.ItemId == item.ItemId &&
                               s.Status != InboundShipmentStatus.Cancelled &&
                               s.Status != InboundShipmentStatus.Failed,
                          cancellationToken);

            if (hasActive)
                return WarehouseErrors.InboundShipment.AlreadyExists(item.ItemId.ToString());
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

        // ── 4a. External carrier — one record per item, each with its own EXT code ─
        if (isExternal)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalCarrierName))
                return WarehouseErrors.InboundShipment.ExternalCarrierNameRequired;

            var externalShipments = new List<InboundShipment>(request.Items.Count);

            foreach (var item in request.Items)
            {
                var extCode = $"EXT-{Guid.NewGuid():N}"[..20];

                var extResult = InboundShipment.Create(
                    itemId:              item.ItemId,
                    sellerId:            _currentUser.UserId,
                    providerCode:        ShippingProviderCode.External,
                    clientOrderCode:     extCode,
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

            WeightGrams    = request.WeightGrams,
            LengthCm       = request.LengthCm,
            WidthCm        = request.WidthCm,
            HeightCm       = request.HeightCm,
            InsuranceValue = request.InsuranceValue,
            CodAmount      = 0,   // inbound — no COD

            GhnHandlingNote = request.GhnHandlingNote,

            // All items declared in a single GHN order
            Items = request.Items.Select(i => new BookShipmentItem
            {
                Name        = itemTitlesDict.GetValueOrDefault(i.ItemId, "Unknown Item"),
                Code        = clientOrderCode,
                Quantity    = 1,
                Price       = i.ItemPrice ?? fallbackPricePerItem,
                WeightGrams = i.WeightGrams
            }).ToList()
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
}