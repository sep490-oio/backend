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
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
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
    private readonly IOutboundShipmentQrTokenService _qrTokenService;
    private readonly IAppInfo         _appInfo;
    private readonly IMediaRelocationService _mediaRelocationService;

    public BookOutboundShipmentCommandHandler(
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
        BookOutboundShipmentCommand request,
        CancellationToken           cancellationToken)
    {
        var now = _clock.UtcNow;

        // ── 1. Load Order (first — drives eligibility + fallbacks) ────────────
        var orderId = OrderId.From(request.OrderId);
        var order = await _dbContext.GetByIdAsync<Order, OrderId>(orderId, cancellationToken: cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order '{request.OrderId}' was not found.");

        // ── 2. Order must be Processing ──────────────────────────────────────
        if (order.Status != OrderStatus.Processing)
            return Error.Validation("order.status", "Order.NotProcessing",
                $"Order '{request.OrderId}' is not in Processing status and cannot be booked for outbound.");

        // ── 3. No active outbound shipment for this order ────────────────────
        var hasActiveOutbound = await _dbContext.Set<OutboundShipment>()
            .AnyAsync(s => s.OrderId == orderId &&
                           s.Status != OutboundShipmentStatus.Cancelled &&
                           s.Status != OutboundShipmentStatus.Failed &&
                           s.Status != OutboundShipmentStatus.Returned &&
                           s.Status != OutboundShipmentStatus.Delivered,
                      cancellationToken);

        if (hasActiveOutbound)
            return WarehouseErrors.OutboundShipment.AlreadyExists(request.OrderId.ToString());

        // ── 4. Load WarehouseItem + verify it belongs to this order's item ───
        var warehouseItemId = WarehouseItemId.From(request.WarehouseItemId);
        var warehouseItem = await _dbContext.GetByIdAsync<WarehouseItem, WarehouseItemId>(
            warehouseItemId,
            cancellationToken: cancellationToken);

        if (warehouseItem is null)
            return WarehouseErrors.WarehouseItem.NotFound(request.WarehouseItemId.ToString());

        // Confirm the warehouse item is for the same auction item as the order.
        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var auctionItemId = auction?.Item?.Id.Value ?? Guid.Empty;
        if (auctionItemId == Guid.Empty || warehouseItem.ItemId != auctionItemId)
            return Error.Validation("warehouseItemId", "WarehouseItem.OrderMismatch",
                "Warehouse item does not belong to this order.");
        var fallbackItemTitle = auction?.Item?.Title.Value ?? "Item";

        // ── 5. Item must be in a shippable status ────────────────────────────
        if (warehouseItem.Status != WarehouseItemStatus.Received &&
            warehouseItem.Status != WarehouseItemStatus.Inspected &&
            warehouseItem.Status != WarehouseItemStatus.Stored)
            return WarehouseErrors.WarehouseItem.NotAvailable;

        // ── 5b. Validate ShipmentMode ────────────────────────────────────────
        var shipmentModeId = string.IsNullOrWhiteSpace(request.ShipmentMode)
            ? OutboundShipmentMode.PlatformManaged.Id
            : request.ShipmentMode;

        if (shipmentModeId != OutboundShipmentMode.PlatformManaged.Id &&
            shipmentModeId != OutboundShipmentMode.ExternalCarrier.Id)
        {
            return Error.Validation("shipmentMode", "BookOutbound.InvalidShipmentMode",
                $"Unknown shipment mode '{request.ShipmentMode}'. Expected 'platform_managed' or 'external_carrier'.");
        }

        var isExternalCarrier = shipmentModeId == OutboundShipmentMode.ExternalCarrier.Id;

        // ── 5c. External-carrier branch ──────────────────────────────────────
        if (isExternalCarrier)
        {
            if (string.IsNullOrWhiteSpace(request.ExternalCarrierName))
                return Error.Validation("externalCarrierName", "BookOutbound.ExternalCarrierNameRequired",
                    "External carrier name is required when shipment mode is 'external_carrier'.");
            if (string.IsNullOrWhiteSpace(request.CarrierTrackingNumber))
                return Error.Validation("carrierTrackingNumber", "BookOutbound.CarrierTrackingNumberRequired",
                    "Carrier tracking number is required when shipment mode is 'external_carrier'.");

            var extWeightGrams = request.WeightGrams > 0 ? request.WeightGrams : 1000;
            var extLengthCm    = request.LengthCm is > 0 ? request.LengthCm : 20;
            var extWidthCm     = request.WidthCm  is > 0 ? request.WidthCm  : 15;
            var extHeightCm    = request.HeightCm is > 0 ? request.HeightCm : 10;

            var extDimensionsResult = PackageDimensions.Create(
                weightGrams: extWeightGrams,
                lengthCm:    extLengthCm,
                widthCm:     extWidthCm,
                heightCm:    extHeightCm);
            if (extDimensionsResult.IsFailure) return extDimensionsResult.Error;

            var extClientOrderCode = $"OUT-{Guid.NewGuid():N}"[..20];

            var extItemPrice = request.ItemPrice > 0m
                ? request.ItemPrice
                : (order.Pricing?.ItemPrice.Amount ?? 0m);
            var extInsuranceValue = request.InsuranceValue > 0m ? request.InsuranceValue : extItemPrice;

            var extShipment = OutboundShipment.Create(
                orderId:            orderId,
                warehouseItemId:    warehouseItemId,
                providerCode:       ShippingProviderCode.External,
                clientOrderCode:    extClientOrderCode,
                dimensions:         extDimensionsResult.Value,
                now:                now,
                shipmentMode:       OutboundShipmentMode.ExternalCarrier,
                externalCarrierName: request.ExternalCarrierName,
                shippingMethod:     request.ShippingMethod,
                shippingFee:        0m,
                insuranceValue:     extInsuranceValue,
                codAmount:          request.CodAmount);

            var extBookedResult = extShipment.RecordBooked(
                carrierTrackingNumber: request.CarrierTrackingNumber!,
                now: now);
            if (extBookedResult.IsFailure) return extBookedResult.Error;

            var extReserveResult = warehouseItem.Reserve(extShipment.Id, now);
            if (extReserveResult.IsFailure) return extReserveResult.Error;

            // Issue QR token for the external-carrier handover. Buyer scans this
            // on package receipt to open a read-only shipment context.
            var nextVersion = extShipment.QrTokenVersion + 1;
            var tokenString = _qrTokenService.Issue(
                shipmentId: extShipment.Id,
                orderId:    order.Id.Value,
                buyerId:    order.BuyerId.Value,
                version:    nextVersion,
                issuedAt:   now);

            var feBase = (_appInfo.FeUrl ?? string.Empty).TrimEnd('/');
            var deepLink = $"{feBase}/orders/{order.Id.Value}/outbound-shipment/receive?token={tokenString}";

            var qrIssueResult = extShipment.IssueQr(qrPayload: deepLink, qrCodeUrl: null, now);
            if (qrIssueResult.IsFailure) return qrIssueResult.Error;

            // ── Evidence photos ────────────────────────────────────────────
            var extEvidenceResult = await AttachEvidenceAsync(extShipment, request, now, cancellationToken);
            if (extEvidenceResult.IsFailure) return extEvidenceResult.Error;

            _dbContext.Insert(extShipment);
            _dbContext.Update(warehouseItem);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return extShipment.ToDto();
        }

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
        // Fall back to the same defaults the detail DTO surfaces so a FE form
        // that submitted 0 / null for any field still produces a valid booking.
        var weightGrams = request.WeightGrams > 0 ? request.WeightGrams : 1000;
        var lengthCm    = request.LengthCm is > 0 ? request.LengthCm : 20;
        var widthCm     = request.WidthCm  is > 0 ? request.WidthCm  : 15;
        var heightCm    = request.HeightCm is > 0 ? request.HeightCm : 10;

        var dimensionsResult = PackageDimensions.Create(
            weightGrams: weightGrams,
            lengthCm:    lengthCm,
            widthCm:     widthCm,
            heightCm:    heightCm);

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
        var recipientName     = string.IsNullOrWhiteSpace(request.RecipientName)     ? (order.Shipping?.RecipientName ?? string.Empty) : request.RecipientName;
        var recipientPhone    = string.IsNullOrWhiteSpace(request.RecipientPhone)    ? (order.Shipping?.Phone ?? string.Empty)         : request.RecipientPhone;
        var recipientAddress  = string.IsNullOrWhiteSpace(request.RecipientAddress)  ? (order.Shipping?.Street ?? order.Shipping?.Address ?? string.Empty) : request.RecipientAddress;
        var recipientWard     = string.IsNullOrWhiteSpace(request.RecipientWard)     ? (order.Shipping?.Ward ?? string.Empty)          : request.RecipientWard;
        var recipientDistrict = string.IsNullOrWhiteSpace(request.RecipientDistrict) ? (order.Shipping?.District ?? string.Empty)      : request.RecipientDistrict;
        var recipientProvince = string.IsNullOrWhiteSpace(request.RecipientProvince) ? (order.Shipping?.City ?? string.Empty)          : request.RecipientProvince;

        // Item manifest — fall back to the order's item title and pricing when
        // the FE didn't send real values.
        var itemName  = string.IsNullOrWhiteSpace(request.ItemName) ? fallbackItemTitle : request.ItemName;
        var itemPrice = request.ItemPrice > 0m
            ? request.ItemPrice
            : (order.Pricing?.ItemPrice.Amount ?? 0m);
        var insuranceValue = request.InsuranceValue > 0m ? request.InsuranceValue : itemPrice;
        var codAmount      = request.CodAmount;

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

            WeightGrams    = weightGrams,
            LengthCm       = lengthCm,
            WidthCm        = widthCm,
            HeightCm       = heightCm,
            InsuranceValue = insuranceValue,
            CodAmount      = codAmount,

            GhnPaymentTypeId = ghnPaymentType.Id,
            GhnHandlingNote  = ghnHandlingNote.Id,
            ExtraDataJson    = request.ExtraDataJson,

            Items =
            [
                new BookShipmentItem
                {
                    Name        = itemName,
                    Code        = clientOrderCode,
                    Quantity    = 1,
                    Price       = itemPrice,
                    WeightGrams = weightGrams
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
            insuranceValue: insuranceValue,
            codAmount:      codAmount,
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

        // Issue QR token for the buyer handover deep-link. Same shape as the
        // external-carrier branch — buyer scans on receipt to open read-only
        // shipment context.
        var pmNextVersion = shipment.QrTokenVersion + 1;
        var pmTokenString = _qrTokenService.Issue(
            shipmentId: shipment.Id,
            orderId:    order.Id.Value,
            buyerId:    order.BuyerId.Value,
            version:    pmNextVersion,
            issuedAt:   now);

        var pmFeBase = (_appInfo.FeUrl ?? string.Empty).TrimEnd('/');
        var pmDeepLink = $"{pmFeBase}/orders/{order.Id.Value}/outbound-shipment/receive?token={pmTokenString}";

        var pmQrIssueResult = shipment.IssueQr(qrPayload: pmDeepLink, qrCodeUrl: null, now);
        if (pmQrIssueResult.IsFailure) return pmQrIssueResult.Error;

        // ── 8b. Evidence photos ──────────────────────────────────────────────
        var evidenceResult = await AttachEvidenceAsync(shipment, request, now, cancellationToken);
        if (evidenceResult.IsFailure) return evidenceResult.Error;

        // ── 9. Persist ────────────────────────────────────────────────────────
        _dbContext.Insert(shipment);
        _dbContext.Update(warehouseItem);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shipment.ToDto();
    }

    private async Task<UnitResult<Error>> AttachEvidenceAsync(
        OutboundShipment shipment,
        BookOutboundShipmentCommand request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var packageIds = request.PackagePhotoMediaUploadIds ?? [];
        var handoverIds = request.HandoverPhotoMediaUploadIds ?? [];

        if (packageIds.Count == 0 && handoverIds.Count == 0)
            return UnitResult.Success<Error>();

        var allIds = packageIds.Concat(handoverIds)
            .Select(MediaUploadId.From)
            .ToList();

        var uploads = await _dbContext.Set<MediaUpload>()
            .Where(x => allIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        if (uploads.Count != allIds.Count)
        {
            var missingIds = allIds
                .Where(id => uploads.All(u => u.Id != id))
                .Select(id => id.Value.ToString());
            return MediaErrors.NotFounds(string.Join(", ", missingIds));
        }

        if (uploads.Any(x => !x.IsConfirmed))
            return MediaErrors.NotConfirm;

        if (uploads.Any(x => x.IsLinked))
            return MediaErrors.AlreadyLinked;

        var packageUploadIds = packageIds.Select(MediaUploadId.From).ToHashSet();

        foreach (var upload in uploads)
        {
            var category = packageUploadIds.Contains(upload.Id)
                ? "staff_package_photo"
                : "staff_handover_photo";

            shipment.AddEvidence(now, category, upload);

            var linkResult = upload.LinkToEntity(shipment.Id, now);
            if (linkResult.IsFailure) return linkResult.Error;

            await _mediaRelocationService.RelocateLinkedUploadAsync(upload, cancellationToken);
        }

        return UnitResult.Success<Error>();
    }
}