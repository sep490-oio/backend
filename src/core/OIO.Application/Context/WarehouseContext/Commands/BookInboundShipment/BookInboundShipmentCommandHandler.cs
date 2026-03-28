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

namespace OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;

internal sealed class BookInboundShipmentCommandHandler
    : ICommandHandler<BookInboundShipmentCommand, InboundShipmentDto>
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

    public async Task<Result<InboundShipmentDto, Error>> Handle(
        BookInboundShipmentCommand request,
        CancellationToken          cancellationToken)
    {
        var now        = _clock.UtcNow;
        var isExternal = request.ShipmentMode == InboundShipmentMode.ExternalCarrier.Id;

        // ── 1. Build package dimensions ───────────────────────────────────────
        var dimensionsResult = PackageDimensions.Create(
            weightGrams: request.WeightGrams,
            lengthCm:    request.LengthCm,
            widthCm:     request.WidthCm,
            heightCm:    request.HeightCm);

        if (dimensionsResult.IsFailure) return dimensionsResult.Error;
        var dimensions = dimensionsResult.Value;

        // ── 1b. Check for existing active inbound shipment for the same item ────── 
        var hasActiveInbound = await _dbContext.Set<InboundShipment>()
            .AnyAsync(s => s.ItemId == request.ItemId &&
                           s.Status != InboundShipmentStatus.Cancelled &&
                           s.Status != InboundShipmentStatus.Failed,
                      cancellationToken);

        if (hasActiveInbound)
            return WarehouseErrors.InboundShipment.AlreadyExists(request.ItemId.ToString());

        // ── 2. Resolve Sender Address ─────────────────────────────────────────
        var senderName      = request.SenderName;
        var senderPhone     = request.SenderPhone;
        var senderAddress   = request.SenderAddress;
        var senderWard      = request.SenderWard;
        var senderDistrict  = request.SenderDistrict;
        var senderProvince  = request.SenderProvince;

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
            {
                return WarehouseErrors.InboundShipment.SenderAddressMissingAndNoDefault;
            }

            senderName     = string.IsNullOrWhiteSpace(senderName) ? defaultUserAddress.Recipient.RecipientName : senderName;
            senderPhone    = string.IsNullOrWhiteSpace(senderPhone) ? defaultUserAddress.Recipient.Phone.Value : senderPhone;
            senderAddress  = string.IsNullOrWhiteSpace(senderAddress) ? defaultUserAddress.Address.Street : senderAddress;
            senderWard     = string.IsNullOrWhiteSpace(senderWard) ? defaultUserAddress.Address.Ward : senderWard;
            senderDistrict = string.IsNullOrWhiteSpace(senderDistrict) ? defaultUserAddress.Address.District : senderDistrict;
            senderProvince = string.IsNullOrWhiteSpace(senderProvince) ? defaultUserAddress.Address.City : senderProvince;
        }

        // ── 3a. External carrier — skip carrier API ───────────────────────────
        if (isExternal)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalCarrierName))
                return WarehouseErrors.InboundShipment.ExternalCarrierNameRequired;

            var externalCode = $"EXT-{Guid.NewGuid():N}"[..20];

            var externalResult = InboundShipment.Create(
                itemId:              request.ItemId,
                sellerId:            _currentUser.UserId,
                providerCode:        ShippingProviderCode.External,
                clientOrderCode:     externalCode,
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

            if (externalResult.IsFailure) return externalResult.Error;

            _dbContext.Insert(externalResult.Value);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return externalResult.Value.ToDto();
        }

        // ── 3b. Platform-managed — load ShippingProviderConfig ────────────────
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

        // ── 4. Generate client order code ─────────────────────────────────────
        var clientOrderCode = $"INB-{Guid.NewGuid():N}"[..20];

        // ── 5. Call carrier API ───────────────────────────────────────────────
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

            Items =
            [
                new BookShipmentItem
                {
                    Name        = request.ItemName,
                    Code        = clientOrderCode,
                    Quantity    = 1,
                    Price       = request.ItemPrice,
                    WeightGrams = request.WeightGrams
                }
            ]
        };

        var bookingResult = await _shippingService.BookShipmentAsync(
            config.ProviderCode.Id, bookingRequest, config, cancellationToken);

        if (bookingResult.IsFailure) return bookingResult.Error;
        var booking = bookingResult.Value;

        // ── 6. Create InboundShipment domain entity ───────────────────────────
        var createResult = InboundShipment.Create(
            itemId:          request.ItemId,
            sellerId:        _currentUser.UserId,
            providerCode:    config.ProviderCode,
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
            senderCarrierAddressData: request.SenderCarrierAddressDataJson is not null
                ? CarrierAddressData.From(request.SenderCarrierAddressDataJson)
                : null,
            shippingFee:       booking.ShippingFee,
            insuranceValue:    request.InsuranceValue,
            notes:             request.Notes,
            expectedArrivalAt: booking.EstimatedDeliveryAt);

        if (createResult.IsFailure) return createResult.Error;
        var shipment = createResult.Value;

        // ── 7. Record carrier booking ─────────────────────────────────────────
        var bookedResult = shipment.RecordBooked(booking.CarrierTrackingNumber, now);
        if (bookedResult.IsFailure) return bookedResult.Error;

        // ── 8. Persist ────────────────────────────────────────────────────────
        _dbContext.Insert(shipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}