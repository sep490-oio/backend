using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Checks.Extensions;
namespace OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;

internal sealed class BookOutboundShipmentCommandHandler
    : ICommandHandler<BookOutboundShipmentCommand, OutboundShipmentDto>
{
    private readonly IDbContext       _dbContext;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ICurrentUser     _currentUser;
    private readonly IShippingService _shippingService;
    private readonly IClock           _clock;

    public BookOutboundShipmentCommandHandler(
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

    public async Task<Result<OutboundShipmentDto, Error>> Handle(
        BookOutboundShipmentCommand request,
        CancellationToken           cancellationToken)
    {
        var now = _clock.UtcNow;

        // ── 1. Load WarehouseItem ─────────────────────────────────────────────
        var warehouseItemId = WarehouseItemId.From(request.WarehouseItemId);

        var warehouseItem = await _dbContext.GetByIdAsync<WarehouseItem, WarehouseItemId>(
            warehouseItemId,
            cancellationToken: cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        if (warehouseItem.Status != WarehouseItemStatus.Received)
            return WarehouseErrors.WarehouseItem.NotAvailable;

        // ── 1b. Check for existing outbound shipment for the order ────────────
        var orderId = OrderId.From(request.OrderId);
        var hasActiveOutbound = await _dbContext.Set<OutboundShipment>()
            .AnyAsync(s => s.OrderId == orderId && 
                           s.Status != OutboundShipmentStatus.Cancelled && 
                           s.Status != OutboundShipmentStatus.Failed && 
                           s.Status != OutboundShipmentStatus.Returned, 
                      cancellationToken);

        if (hasActiveOutbound)
            return WarehouseErrors.OutboundShipment.AlreadyExists(request.OrderId.ToString());

        // ── 1c. Load Order for fallback shipping address ──────────────────────
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(orderId, cancellationToken: cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order '{request.OrderId}' was not found.");

        // ── 2. Load ShippingProviderConfig ────────────────────────────────────
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

        // ── 3. Build package dimensions ───────────────────────────────────────
        var dimensionsResult = PackageDimensions.Create(
            weightGrams: request.WeightGrams,
            lengthCm:    request.LengthCm,
            widthCm:     request.WidthCm,
            heightCm:    request.HeightCm);

        if (dimensionsResult.IsFailure) return dimensionsResult.Error;
        var dimensions = dimensionsResult.Value;

        // ── 4. Generate client order code ─────────────────────────────────────
        var clientOrderCode = $"OUT-{Guid.NewGuid():N}"[..20];

        // ── 5. Resolve GHN enums from string inputs ───────────────────────────
        var ghnPaymentType  = string.IsNullOrWhiteSpace(request.GhnPaymentTypeId)
            ? GhnPaymentType.ShopPays
            : GhnPaymentType.FromId(request.GhnPaymentTypeId).GetValueOrDefault(GhnPaymentType.ShopPays);

        var ghnHandlingNote = string.IsNullOrWhiteSpace(request.GhnHandlingNote)
            ? GhnHandlingNote.AllowTry
            : GhnHandlingNote.FromId(request.GhnHandlingNote).GetValueOrDefault(GhnHandlingNote.AllowTry);

        // ── 6. Call carrier API ───────────────────────────────────────────────
        // Sender   = warehouse (pick* fields from config)
        // Recipient = buyer address (from command, fallback to order)
        var recipientName     = string.IsNullOrWhiteSpace(request.RecipientName) ? order.Shipping.RecipientName : request.RecipientName;
        var recipientPhone    = string.IsNullOrWhiteSpace(request.RecipientPhone) ? order.Shipping.Phone : request.RecipientPhone;
        var recipientAddress  = string.IsNullOrWhiteSpace(request.RecipientAddress) ? order.Shipping.Address : request.RecipientAddress;
        var recipientWard     = string.IsNullOrWhiteSpace(request.RecipientWard) ? order.Shipping.Ward : request.RecipientWard;
        var recipientDistrict = string.IsNullOrWhiteSpace(request.RecipientDistrict) ? order.Shipping.District : request.RecipientDistrict;
        var recipientProvince = string.IsNullOrWhiteSpace(request.RecipientProvince) ? order.Shipping.City : request.RecipientProvince;

        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = clientOrderCode,

            // Buyer is the delivery destination
            RecipientName                  = recipientName,
            RecipientPhone                 = recipientPhone,
            RecipientAddress               = recipientAddress,
            RecipientWard                  = recipientWard,
            RecipientDistrict              = recipientDistrict,
            RecipientProvince              = recipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson,

            // Warehouse is the pickup point — no sender override needed
            // GHN uses the shop address registered in the portal (matched by ShopId in token)
            SenderName    = null,
            SenderPhone   = null,
            SenderAddress = null,

            WeightGrams    = request.WeightGrams,
            LengthCm       = request.LengthCm,
            WidthCm        = request.WidthCm,
            HeightCm       = request.HeightCm,
            InsuranceValue = request.InsuranceValue,
            CodAmount      = request.CodAmount,

            GhnPaymentTypeId = ghnPaymentType.Id,
            GhnHandlingNote  = ghnHandlingNote.Id,
            ExtraDataJson    = request.ExtraDataJson,

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

        // ── 7. Create OutboundShipment ────────────────────────────────────────
        var shipment = OutboundShipment.Create(
            orderId:        OrderId.From(request.OrderId),
            warehouseItemId: warehouseItemId,
            providerCode:   config.ProviderCode,
            clientOrderCode: clientOrderCode,
            dimensions:     dimensions,
            now:            now,
            shippingMethod: request.ShippingMethod,
            recipientCarrierAddressData: request.RecipientCarrierAddressDataJson is not null
                ? CarrierAddressData.From(request.RecipientCarrierAddressDataJson)
                : null,
            shippingFee:    booking.ShippingFee,
            insuranceValue: request.InsuranceValue,
            codAmount:      request.CodAmount,
            ghnPaymentType: ghnPaymentType,
            ghnHandlingNote: ghnHandlingNote);

        // ── 8. Record booking + reserve item ──────────────────────────────────
        var bookedResult = shipment.RecordBooked(
            booking.CarrierTrackingNumber,
            now,
            booking.ShippingLabelUrl,
            booking.EstimatedDeliveryAt);

        if (bookedResult.IsFailure) return bookedResult.Error;

        var reserveResult = warehouseItem.Reserve(shipment.Id, now);
        if (reserveResult.IsFailure) return reserveResult.Error;

        // ── 9. Persist ────────────────────────────────────────────────────────
        _dbContext.Insert(shipment);
        _dbContext.Update(warehouseItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }
}