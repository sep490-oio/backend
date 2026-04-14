using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Shipping;
using OIO.Application.Context.MediaContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Context.WarehouseContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;

internal sealed class BookGhnOutboundShipmentCommandHandler
    : ICommandHandler<BookGhnOutboundShipmentCommand, OutboundShipmentDto>
{
    private readonly IDbContext       _dbContext;
    private readonly IUnitOfWork      _unitOfWork;
    private readonly ICurrentUser     _currentUser;
    private readonly IShippingService _shippingService;
    private readonly IClock           _clock;
    private readonly IOutboundShipmentQrTokenService _qrTokenService;
    private readonly IAppInfo         _appInfo;
    private readonly IMediaRelocationService _mediaRelocationService;

    public BookGhnOutboundShipmentCommandHandler(
        IDbContext       dbContext,
        IUnitOfWork      unitOfWork,
        ICurrentUser     currentUser,
        IShippingService shippingService,
        IClock           clock,
        IOutboundShipmentQrTokenService qrTokenService,
        IAppInfo         appInfo,
        IMediaRelocationService mediaRelocationService)
    {
        _dbContext       = dbContext;
        _unitOfWork      = unitOfWork;
        _currentUser     = currentUser;
        _shippingService = shippingService;
        _clock           = clock;
        _qrTokenService  = qrTokenService;
        _appInfo         = appInfo;
        _mediaRelocationService = mediaRelocationService;
    }

    public async Task<Result<OutboundShipmentDto, Error>> Handle(
        BookGhnOutboundShipmentCommand request,
        CancellationToken           cancellationToken)
    {
        var now = _clock.UtcNow;

        // ── 1. Load Order ────────────────────────────────────────────────────
        var orderId = OrderId.From(request.OrderId);
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(orderId, cancellationToken: cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order '{request.OrderId}' not found.");

        if (order.Status != OrderStatus.Processing)
            return Error.Validation("order.status", "Order.NotProcessing", "Order is not in Processing status.");

        // ── 2. No active outbound shipment ───────────────────────────────────
        var hasActive = await _dbContext.Set<OutboundShipment>()
            .AnyAsync(s => s.OrderId == orderId &&
                           s.Status != OutboundShipmentStatus.Cancelled &&
                           s.Status != OutboundShipmentStatus.Failed &&
                           s.Status != OutboundShipmentStatus.Returned &&
                           s.Status != OutboundShipmentStatus.Delivered,
                      cancellationToken);

        if (hasActive)
            return WarehouseErrors.OutboundShipment.AlreadyExists(request.OrderId.ToString());

        // ── 3. Load WarehouseItem ─────────────────────────────────────────────
        var warehouseItemId = WarehouseItemId.From(request.WarehouseItemId);
        var warehouseItem = await _dbContext.GetByIdAsync<WarehouseItem, WarehouseItemId>(
            warehouseItemId,
            cancellationToken: cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);
        
        var fallbackItemTitle = auction?.Item?.Title.Value ?? "Item";

        if (warehouseItem.ItemId != auction?.Item?.Id.Value)
        {
            return Error.Validation("warehouseItemId", "WarehouseItem.OrderMismatch",
                "Warehouse item does not belong to the item ordered in this auction.");
        }

        // ── 4. Shipment Mode Parity ──────────────────────────────────────────
        var shipmentModeId = string.IsNullOrWhiteSpace(request.ShipmentMode)
            ? OutboundShipmentMode.PlatformManaged.Id
            : request.ShipmentMode;

        if (shipmentModeId == OutboundShipmentMode.ExternalCarrier.Id)
        {
            return await HandleExternalCarrierAsync(request, order, warehouseItem, now, cancellationToken);
        }

        // ── 5. GHN Specialized Flow (Platform Managed) ────────────────────────
        var config = await _dbContext.Set<ShippingProviderConfig>()
            .FirstOrDefaultAsync(c => c.ProviderCode == ShippingProviderCode.Ghn && c.IsActive, cancellationToken);

        if (config is null)
            return WarehouseErrors.ShippingProvider.NotFound("ghn");

        var weightGrams = request.WeightGrams > 0 ? request.WeightGrams : 1000;
        var lengthCm    = request.LengthCm is > 0 ? request.LengthCm : 20;
        var widthCm     = request.WidthCm  is > 0 ? request.WidthCm  : 15;
        var heightCm    = request.HeightCm is > 0 ? request.HeightCm : 10;

        var dimensionsResult = PackageDimensions.Create(weightGrams, lengthCm, widthCm, heightCm);
        if (dimensionsResult.IsFailure) return dimensionsResult.Error;

        // Metadata merging
        var metadataJson = JsonSerializer.Serialize(new 
        { 
            ghn_sender_metadata = request.SenderMetadata,
            ghn_recipient_metadata = request.RecipientMetadata
        });

        var clientOrderCode = $"OUT-GHN-{Guid.NewGuid():N}"[..20];
        
        var recipientName     = string.IsNullOrWhiteSpace(request.RecipientName)     ? (order.Shipping?.RecipientName ?? "") : request.RecipientName;
        var recipientPhone    = string.IsNullOrWhiteSpace(request.RecipientPhone)    ? (order.Shipping?.Phone ?? "")         : request.RecipientPhone;
        var recipientAddress  = string.IsNullOrWhiteSpace(request.RecipientAddress)  ? (order.Shipping?.Street ?? order.Shipping?.Address ?? "") : request.RecipientAddress;
        var recipientWard     = string.IsNullOrWhiteSpace(request.RecipientWard)     ? (order.Shipping?.Ward ?? "")          : request.RecipientWard;
        var recipientDistrict = string.IsNullOrWhiteSpace(request.RecipientDistrict) ? (order.Shipping?.District ?? "")      : request.RecipientDistrict;
        var recipientProvince = string.IsNullOrWhiteSpace(request.RecipientProvince) ? (order.Shipping?.City ?? "")          : request.RecipientProvince;

        var itemName  = string.IsNullOrWhiteSpace(request.ItemName) ? fallbackItemTitle : request.ItemName;
        var itemPrice = request.ItemPrice > 0m ? request.ItemPrice : (order.Pricing?.ItemPrice.Amount ?? 0m);
        var insuranceValue = request.InsuranceValue > 0m ? request.InsuranceValue : itemPrice;

        var ghnPaymentType  = GhnPaymentType.FromId(request.GhnPaymentTypeId ?? "").GetValueOrDefault(GhnPaymentType.ShopPays);
        var ghnHandlingNote = GhnHandlingNote.FromId(request.GhnHandlingNote ?? "").GetValueOrDefault(GhnHandlingNote.AllowTry);

        // ── 5e. Resolve Sender Address (Fallback to Seller's default) ────────
        var senderName     = request.SenderName;
        var senderPhone    = request.SenderPhone;
        var senderAddress  = request.SenderAddress;
        var senderWard     = request.SenderWard;
        var senderDistrict = request.SenderDistrict;
        var senderProvince = request.SenderProvince;

        if (string.IsNullOrWhiteSpace(senderName)    ||
            string.IsNullOrWhiteSpace(senderPhone)   ||
            string.IsNullOrWhiteSpace(senderAddress))
        {
            var sellerId = auction?.Item?.SellerId;
            if (sellerId.HasValue)
            {
                var defaultUserAddress = await _dbContext.Set<OIO.Domain.Context.UserContext.Aggregates.Users.UserAddress>()
                    .FirstOrDefaultAsync(a => a.UserId == sellerId.Value && a.IsDefault, cancellationToken);

                if (defaultUserAddress is not null)
                {
                    senderName     = string.IsNullOrWhiteSpace(senderName)     ? defaultUserAddress.Recipient.RecipientName : senderName;
                    senderPhone    = string.IsNullOrWhiteSpace(senderPhone)    ? defaultUserAddress.Recipient.Phone.Value    : senderPhone;
                    senderAddress  = string.IsNullOrWhiteSpace(senderAddress)  ? defaultUserAddress.Address.Street          : senderAddress;
                    senderWard     = string.IsNullOrWhiteSpace(senderWard)     ? defaultUserAddress.Address.Ward            : senderWard;
                    senderDistrict = string.IsNullOrWhiteSpace(senderDistrict) ? defaultUserAddress.Address.District        : senderDistrict;
                    senderProvince = string.IsNullOrWhiteSpace(senderProvince) ? defaultUserAddress.Address.City            : senderProvince;
                }
            }
        }

        var bookingRequest = new BookShipmentRequest
        {
            ClientOrderCode = clientOrderCode,
            RecipientName   = recipientName,
            RecipientPhone  = recipientPhone,
            RecipientAddress = recipientAddress,
            RecipientWard    = recipientWard,
            RecipientDistrict = recipientDistrict,
            RecipientProvince = recipientProvince,
            RecipientCarrierAddressDataJson = request.RecipientCarrierAddressDataJson,

            SenderName     = senderName,
            SenderPhone    = senderPhone,
            SenderAddress  = senderAddress,
            SenderWard     = senderWard,
            SenderDistrict = senderDistrict,
            SenderProvince = senderProvince,

            WeightGrams    = weightGrams,
            LengthCm       = lengthCm,
            WidthCm        = widthCm,
            HeightCm       = heightCm,
            InsuranceValue = insuranceValue,
            CodAmount      = request.CodAmount,

            GhnPaymentTypeId = ghnPaymentType.Id,
            GhnHandlingNote  = ghnHandlingNote.Id,
            ExtraDataJson    = metadataJson,

            Items = [ new BookShipmentItem { Name = itemName, Code = clientOrderCode, Quantity = 1, Price = itemPrice, WeightGrams = weightGrams } ]
        };

        var bookingResult = await _shippingService.BookShipmentAsync("ghn", bookingRequest, config, cancellationToken);
        if (bookingResult.IsFailure) return bookingResult.Error;
        var booking = bookingResult.Value;

        var shipment = OutboundShipment.Create(
            orderId:        orderId,
            warehouseItemId: warehouseItemId,
            providerCode:   ShippingProviderCode.Ghn,
            clientOrderCode: clientOrderCode,
            dimensions:     dimensionsResult.Value,
            now:            now,
            shippingMethod: request.ShippingMethod,
            recipientCarrierAddressData: request.RecipientCarrierAddressDataJson is not null ? CarrierAddressData.From(request.RecipientCarrierAddressDataJson) : null,
            shippingFee:    booking.ShippingFee,
            insuranceValue: insuranceValue,
            codAmount:      request.CodAmount,
            ghnPaymentType: ghnPaymentType,
            ghnHandlingNote: ghnHandlingNote);

        shipment.RecordBooked(booking.CarrierTrackingNumber, now, booking.ShippingLabelUrl, booking.EstimatedDeliveryAt);
        warehouseItem.Reserve(shipment.Id, now);

        await IssueQrAndAttachEvidenceAsync(shipment, warehouseItem, order, request, now, cancellationToken);

        _dbContext.Insert(shipment);
        _dbContext.Update(warehouseItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }

    private async Task<Result<OutboundShipmentDto, Error>> HandleExternalCarrierAsync(
        BookGhnOutboundShipmentCommand request, 
        Order order, 
        WarehouseItem warehouseItem, 
        DateTime now, 
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ExternalCarrierName))
            return Error.Validation("externalCarrierName", "BookOutbound.ExternalCarrierNameRequired", "External carrier name is required.");
        if (string.IsNullOrWhiteSpace(request.CarrierTrackingNumber))
            return Error.Validation("carrierTrackingNumber", "BookOutbound.CarrierTrackingNumberRequired", "Carrier tracking number is required.");

        var extWeightGrams = request.WeightGrams > 0 ? request.WeightGrams : 1000;
        var extLengthCm    = request.LengthCm is > 0 ? request.LengthCm : 20;
        var extWidthCm     = request.WidthCm  is > 0 ? request.WidthCm  : 15;
        var extHeightCm    = request.HeightCm is > 0 ? request.HeightCm : 10;

        var extDimensionsResult = PackageDimensions.Create(extWeightGrams, extLengthCm, extWidthCm, extHeightCm);
        if (extDimensionsResult.IsFailure) return extDimensionsResult.Error;

        var extItemPrice = request.ItemPrice > 0m ? request.ItemPrice : (order.Pricing?.ItemPrice.Amount ?? 0m);
        var extInsuranceValue = request.InsuranceValue > 0m ? request.InsuranceValue : extItemPrice;

        var extShipment = OutboundShipment.Create(
            orderId:            order.Id,
            warehouseItemId:    warehouseItem.Id,
            providerCode:       ShippingProviderCode.External,
            clientOrderCode:    $"OUT-{Guid.NewGuid():N}"[..20],
            dimensions:         extDimensionsResult.Value,
            now:                now,
            shipmentMode:       OutboundShipmentMode.ExternalCarrier,
            externalCarrierName: request.ExternalCarrierName,
            shippingMethod:     request.ShippingMethod,
            shippingFee:        0m,
            insuranceValue:     extInsuranceValue,
            codAmount:          request.CodAmount);

        extShipment.RecordBooked(request.CarrierTrackingNumber, now);
        warehouseItem.Reserve(extShipment.Id, now);

        await IssueQrAndAttachEvidenceAsync(extShipment, warehouseItem, order, request, now, cancellationToken);

        _dbContext.Insert(extShipment);
        _dbContext.Update(warehouseItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return extShipment.ToDto();
    }

    private async Task IssueQrAndAttachEvidenceAsync(
        OutboundShipment shipment, 
        WarehouseItem warehouseItem, 
        Order order, 
        BookGhnOutboundShipmentCommand request, 
        DateTime now, 
        CancellationToken cancellationToken)
    {
        // QR token
        var nextVersion = shipment.QrTokenVersion + 1;
        var tokenString = _qrTokenService.Issue(shipment.Id, order.Id.Value, order.BuyerId.Value, nextVersion, now);
        var feBase = (_appInfo.FeUrl ?? "").TrimEnd('/');
        var deepLink = $"{feBase}/orders/{order.Id.Value}/outbound-shipment/receive?token={tokenString}";
        shipment.IssueQr(deepLink, null, now);

        // Evidence
        var packageIds = request.PackagePhotoMediaUploadIds ?? [];
        var handoverIds = request.HandoverPhotoMediaUploadIds ?? [];
        if (packageIds.Count > 0 || handoverIds.Count > 0)
        {
            var allIds = packageIds.Concat(handoverIds).Select(MediaUploadId.From).ToList();
            var uploads = await _dbContext.Set<MediaUpload>().Where(x => allIds.Contains(x.Id)).ToListAsync(cancellationToken);
            var packageUploadIds = packageIds.Select(MediaUploadId.From).ToHashSet();
            foreach (var upload in uploads)
            {
                var category = packageUploadIds.Contains(upload.Id) ? "staff_package_photo" : "staff_handover_photo";
                shipment.AddEvidence(now, category, upload);
                upload.LinkToEntity(shipment.Id, now);
                await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
            }
        }
    }
}
