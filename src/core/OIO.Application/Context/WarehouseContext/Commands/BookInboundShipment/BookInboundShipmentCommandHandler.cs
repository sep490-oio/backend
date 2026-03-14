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

        // ── 2a. External carrier — skip carrier API ───────────────────────────
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
                senderName:          request.SenderName,
                senderPhone:         request.SenderPhone,
                senderAddress:       request.SenderAddress,
                senderWard:          request.SenderWard,
                senderDistrict:      request.SenderDistrict,
                senderProvince:      request.SenderProvince,
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

        // ── 2b. Platform-managed — load ShippingProviderConfig ────────────────
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

        // ── 3. Generate client order code ─────────────────────────────────────
        var clientOrderCode = $"INB-{Guid.NewGuid():N}"[..20];

        // ── 4. Call carrier API ───────────────────────────────────────────────
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
            SenderName                   = request.SenderName,
            SenderPhone                  = request.SenderPhone,
            SenderAddress                = request.SenderAddress,
            SenderWard                   = request.SenderWard,
            SenderDistrict               = request.SenderDistrict,
            SenderProvince               = request.SenderProvince,
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

        // ── 5. Create InboundShipment domain entity ───────────────────────────
        var createResult = InboundShipment.Create(
            itemId:          request.ItemId,
            sellerId:        _currentUser.UserId,
            providerCode:    config.ProviderCode,
            clientOrderCode: clientOrderCode,
            senderName:      request.SenderName,
            senderPhone:     request.SenderPhone,
            senderAddress:   request.SenderAddress,
            senderWard:      request.SenderWard,
            senderDistrict:  request.SenderDistrict,
            senderProvince:  request.SenderProvince,
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

        // ── 6. Record carrier booking ─────────────────────────────────────────
        var bookedResult = shipment.RecordBooked(booking.CarrierTrackingNumber, now);
        if (bookedResult.IsFailure) return bookedResult.Error;

        // ── 7. Persist ────────────────────────────────────────────────────────
        _dbContext.Insert(shipment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}