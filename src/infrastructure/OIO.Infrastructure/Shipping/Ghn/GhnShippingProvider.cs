using System.Net.Http.Json;
using System.Text.Json;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Shipping.Ghn;

/// <summary>
/// Anti-corruption adapter for Giao Hàng Nhanh (GHN).
///
/// Auth:        Token header + ShopId header (static — no token refresh needed).
/// Weight:      Grams (domain stores grams; no conversion needed).
/// Our ref:     client_order_code → stored in ClientOrderCode field.
/// Their ref:   order_code        → stored in CarrierTrackingNumber.
/// Webhook:     JSON push, no HMAC. Verified by matching ShopId in payload.
///
/// Sandbox base URL: https://dev-online-gateway.ghn.vn
/// Prod base URL:    https://online-gateway.ghn.vn
/// </summary>
internal sealed class GhnShippingProvider : IShippingProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GhnShippingProvider> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ProviderCode => "ghn";

    public GhnShippingProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<GhnShippingProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // ============================================================
    // CreateShipmentAsync
    // ============================================================

    public async Task<Result<CreateShipmentResponse, Error>> CreateShipmentAsync(
        CreateShipmentRequest request,
        ShippingProviderConfig config,
        CancellationToken ct = default)
    {
        var credsResult = ParseCredentials(config);
        if (credsResult.IsFailure) return credsResult.Error;

        var creds = credsResult.Value;

        // Parse recipient GHN address IDs from CarrierAddressData JSON
        var addrResult = ParseCarrierAddressData(request.RecipientCarrierAddressDataJson, "RecipientCarrierAddressDataJson");
        if (addrResult.IsFailure) return addrResult.Error;

        var (toDistrictId, toWardCode) = addrResult.Value;

        var ghnRequest = new GhnCreateOrderRequest
        {
            PaymentTypeId   = ParsePaymentTypeId(request.GhnPaymentTypeId),
            RequiredNote    = string.IsNullOrWhiteSpace(request.GhnHandlingNote)
                                  ? "CHOTHUHANG"
                                  : request.GhnHandlingNote,
            ClientOrderCode = request.ClientOrderCode,

            ToName       = request.RecipientName,
            ToPhone      = request.RecipientPhone,
            ToAddress    = request.RecipientAddress,
            ToWardCode   = toWardCode,
            ToDistrictId = toDistrictId,
            FromName         = request.SenderName,
            FromPhone        = request.SenderPhone,
            FromAddress      = request.SenderAddress,
            FromWardName     = request.SenderWard,
            FromDistrictName = request.SenderDistrict,
            Weight = request.WeightGrams,  // GHN uses grams — no conversion
            Length = request.LengthCm,
            Width  = request.WidthCm,
            Height = request.HeightCm,

            InsuranceValue = request.InsuranceValue,
            CodAmount      = request.CodAmount,

            ServiceTypeId = ParseServiceTypeId(request.ExtraDataJson),

            Items = request.Items.Select(i => new GhnOrderItem
            {
                Name     = i.Name,
                Code     = i.Code,
                Quantity = i.Quantity,
                Price    = (int)i.Price,
                Weight   = i.WeightGrams,
                Length   = i.LengthCm,
                Width    = i.WidthCm,
                Height   = i.HeightCm
            }).ToList()
        };

        using var http = BuildClient(config, creds);

        try
        {
            var response = await http.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/create",
                ghnRequest,
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GHN CreateOrder HTTP {StatusCode}: {Body}", response.StatusCode, body);
                return Error.Unexpected(
                    code: "Ghn.CreateOrder.HttpError",
                    description: $"GHN returned HTTP {(int)response.StatusCode}: {body}");
            }

            var result = JsonSerializer.Deserialize<GhnCreateOrderResponse>(body, _jsonOptions);

            if (result is null || result.Code != 200 || result.Data is null)
            {
                _logger.LogWarning("GHN CreateOrder failed: code={Code} msg={Message}", result?.Code, result?.Message);
                return Error.Unexpected(
                    code: "Ghn.CreateOrder.ApiError",
                    description: $"GHN error {result?.Code}: {result?.Message}");
            }

            DateTime? estimatedDelivery = null;
            if (!string.IsNullOrWhiteSpace(result.Data.ExpectedDeliveryTime) &&
                DateTime.TryParse(result.Data.ExpectedDeliveryTime, out var parsed))
            {
                estimatedDelivery = parsed.ToUniversalTime();
            }

            return new CreateShipmentResponse
            {
                CarrierTrackingNumber = result.Data.OrderCode,
                ShippingLabelUrl      = null,  // GHN does not return label URL from create — use print API separately
                ShippingFee           = result.Data.TotalFee,
                EstimatedDeliveryAt   = estimatedDelivery
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CreateShipmentAsync exception");
            return Error.Unexpected(
                code: "Ghn.CreateOrder.Exception",
                description: ex.Message);
        }
    }

    // ============================================================
    // CancelShipmentAsync
    // ============================================================

    public async Task<Result<Unit, Error>> CancelShipmentAsync(
        string carrierTrackingNumber,
        ShippingProviderConfig config,
        CancellationToken ct = default)
    {
        var credsResult = ParseCredentials(config);
        if (credsResult.IsFailure) return credsResult.Error;

        var creds = credsResult.Value;

        var ghnRequest = new GhnCancelOrderRequest
        {
            OrderCodes = [carrierTrackingNumber]
        };

        using var http = BuildClient(config, creds);

        try
        {
            var response = await http.PostAsJsonAsync(
                "/shiip/public-api/v2/switch-status/cancel",
                ghnRequest,
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("GHN CancelOrder HTTP {StatusCode}: {Body}", response.StatusCode, body);
                return Error.Unexpected(
                    code: "Ghn.CancelOrder.HttpError",
                    description: $"GHN returned HTTP {(int)response.StatusCode}: {body}");
            }

            var result = JsonSerializer.Deserialize<GhnCancelOrderResponse>(body, _jsonOptions);

            if (result is null || result.Code != 200)
            {
                _logger.LogWarning("GHN CancelOrder failed: code={Code} msg={Message}", result?.Code, result?.Message);
                return Error.Unexpected(
                    code: "Ghn.CancelOrder.ApiError",
                    description: $"GHN error {result?.Code}: {result?.Message}");
            }

            // Check per-order result
            var orderResult = result.Data?.FirstOrDefault(r => r.OrderCode == carrierTrackingNumber);
            if (orderResult is not null && !orderResult.Result)
            {
                return Error.Unexpected(
                    code: "Ghn.CancelOrder.Rejected",
                    description: orderResult.Message ?? "GHN rejected the cancellation.");
            }

            return Unit.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CancelShipmentAsync exception");
            return Error.Unexpected(
                code: "Ghn.CancelOrder.Exception",
                description: ex.Message);
        }
    }

    // ============================================================
    // CalculateFeeAsync
    // ============================================================

    public async Task<Result<decimal, Error>> CalculateFeeAsync(
        CalculateFeeRequest request,
        ShippingProviderConfig config,
        CancellationToken ct = default)
    {
        var credsResult = ParseCredentials(config);
        if (credsResult.IsFailure) return credsResult.Error;

        var creds = credsResult.Value;

        var addrResult = ParseCarrierAddressData(request.RecipientCarrierAddressDataJson, "RecipientCarrierAddressDataJson");
        if (addrResult.IsFailure) return addrResult.Error;

        var (toDistrictId, toWardCode) = addrResult.Value;

        var ghnRequest = new GhnCalculateFeeRequest
        {
            ToWardCode    = toWardCode,
            ToDistrictId  = toDistrictId,
            Weight        = request.WeightGrams,
            Length        = request.LengthCm,
            Width         = request.WidthCm,
            Height        = request.HeightCm,
            InsuranceValue = request.InsuranceValue
            // ServiceTypeId defaults to null → GHN uses service_type_id = 2 (standard express)
        };

        using var http = BuildClient(config, creds);

        try
        {
            var response = await http.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/fee",
                ghnRequest,
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return Error.Unexpected(
                    code: "Ghn.CalculateFee.HttpError",
                    description: $"GHN returned HTTP {(int)response.StatusCode}: {body}");

            var result = JsonSerializer.Deserialize<GhnCalculateFeeResponse>(body, _jsonOptions);

            if (result is null || result.Code != 200 || result.Data is null)
                return Error.Unexpected(
                    code: "Ghn.CalculateFee.ApiError",
                    description: $"GHN error {result?.Code}: {result?.Message}");

            return (decimal)result.Data.Total;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CalculateFeeAsync exception");
            return Error.Unexpected(
                code: "Ghn.CalculateFee.Exception",
                description: ex.Message);
        }
    }

    // ============================================================
    // CalculateExpectedDeliveryTimeAsync
    // ============================================================

    public async Task<Result<DateTime?, Error>> CalculateExpectedDeliveryTimeAsync(
        CalculateExpectedDeliveryTimeRequest request,
        ShippingProviderConfig config,
        CancellationToken ct = default)
    {
        var credsResult = ParseCredentials(config);
        if (credsResult.IsFailure) return credsResult.Error;

        var creds = credsResult.Value;

        var toAddrResult = ParseCarrierAddressData(request.RecipientCarrierAddressDataJson, "RecipientCarrierAddressDataJson");
        if (toAddrResult.IsFailure) return toAddrResult.Error;
        var (toDistrictId, toWardCode) = toAddrResult.Value;

        var fromAddrResult = ParseCarrierAddressData(request.SenderCarrierAddressDataJson, "SenderCarrierAddressDataJson");
        if (fromAddrResult.IsFailure) return fromAddrResult.Error;
        var (fromDistrictId, fromWardCode) = fromAddrResult.Value;

        var ghnRequest = new GhnExpectedDeliveryTimeRequest
        {
            FromDistrictId = fromDistrictId,
            FromWardCode   = fromWardCode,
            ToDistrictId   = toDistrictId,
            ToWardCode     = toWardCode
        };

        using var http = BuildClient(config, creds);

        try
        {
            var response = await http.PostAsJsonAsync(
                "/shiip/public-api/v2/shipping-order/leadtime",
                ghnRequest,
                ct);

            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                return Error.Unexpected(
                    code: "Ghn.CalculateExpectedDeliveryTime.HttpError",
                    description: $"GHN returned HTTP {(int)response.StatusCode}: {body}");

            var result = JsonSerializer.Deserialize<GhnExpectedDeliveryTimeResponse>(body, _jsonOptions);

            if (result is null || result.Code != 200 || result.Data is null)
                return Error.Unexpected(
                    code: "Ghn.CalculateExpectedDeliveryTime.ApiError",
                    description: $"GHN error {result?.Code}: {result?.Message}");

            var estimatedDelivery = DateTimeOffset.FromUnixTimeSeconds(result.Data.Leadtime).UtcDateTime;
            return estimatedDelivery;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GHN CalculateExpectedDeliveryTimeAsync exception");
            return Error.Unexpected(
                code: "Ghn.CalculateExpectedDeliveryTime.Exception",
                description: ex.Message);
        }
    }

    // ============================================================
    // ParseWebhook
    // GHN pushes JSON. No HMAC signature.
    // Verification: check ShopId in payload matches our config.
    // ============================================================

    public Result<ParsedWebhookEvent, Error> ParseWebhook(
        string body,
        IHeaderDictionary headers,
        IQueryCollection query,
        ShippingProviderConfig config)
    {
        var credsResult = ParseCredentials(config);
        if (credsResult.IsFailure) return credsResult.Error;

        var creds = credsResult.Value;

        GhnWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<GhnWebhookPayload>(body, _jsonOptions);
        }
        catch (JsonException ex)
        {
            return Error.Unexpected(
                code: "Ghn.Webhook.ParseError",
                description: $"Failed to deserialize GHN webhook: {ex.Message}");
        }

        if (payload is null)
            return Error.Unexpected(
                code: "Ghn.Webhook.EmptyPayload",
                description: "GHN webhook body deserialized to null.");

        // Verify ShopId matches our config — this is the only auth GHN provides
        if (payload.ShopId != creds.ShopId)
        {
            _logger.LogWarning(
                "GHN webhook ShopId mismatch: expected {Expected}, got {Got}",
                creds.ShopId, payload.ShopId);
            return Error.Unexpected(
                code: "Ghn.Webhook.ShopIdMismatch",
                description: $"GHN webhook ShopId {payload.ShopId} does not match configured ShopId {creds.ShopId}.");
        }

        // Parse event time — try Time field first, fall back to Timestamp (Unix)
        DateTime eventTime;
        if (!string.IsNullOrWhiteSpace(payload.Time) &&
            DateTime.TryParse(payload.Time, out var parsedTime))
        {
            eventTime = parsedTime.ToUniversalTime();
        }
        else if (payload.Timestamp.HasValue)
        {
            eventTime = DateTimeOffset.FromUnixTimeSeconds(payload.Timestamp.Value).UtcDateTime;
        }
        else
        {
            eventTime = DateTime.UtcNow;
        }

        var normalized = NormalizeGhnStatus(payload.Status);

        // Location: warehouse name + address if available
        var location = string.IsNullOrWhiteSpace(payload.WarehouseAddress)
            ? payload.Warehouse
            : $"{payload.Warehouse} - {payload.WarehouseAddress}";

        return new ParsedWebhookEvent
        {
            ProviderCode         = ProviderCode,
            ClientOrderCode      = payload.ClientOrderCode,
            CarrierTrackingNumber = payload.OrderCode,
            CarrierStatusRaw     = payload.Status,
            CarrierStatusDesc    = payload.Description,
            NormalizedStatus     = normalized,
            Location             = location,
            ReasonCode           = null,
            ReasonDescription    = payload.Reason,
            EventTime            = eventTime,
            RawPayloadJson       = body
        };
    }

    // ============================================================
    // Private helpers
    // ============================================================

    private static Result<GhnCredentials, Error> ParseCredentials(ShippingProviderConfig config)
    {
        try
        {
            var creds = JsonSerializer.Deserialize<GhnCredentials>(
                config.Credentials.RawJson,
                _jsonOptions);

            if (creds is null || string.IsNullOrWhiteSpace(creds.Token) || creds.ShopId == 0)
                return Error.Unexpected(
                    code: "Ghn.Credentials.Invalid",
                    description: "GHN credentials are missing token or shop_id.");

            return creds;
        }
        catch (JsonException ex)
        {
            return Error.Unexpected(
                code: "Ghn.Credentials.ParseError",
                description: $"Failed to parse GHN credentials: {ex.Message}");
        }
    }

    /// <summary>
    /// Parses {"district_id": 1442, "ward_code": "21012"} from CarrierAddressData JSON.
    /// GHN requires these IDs — plain text addresses are not accepted.
    /// </summary>
    private static Result<(int districtId, string wardCode), Error> ParseCarrierAddressData(
        string? json,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Error.Validation(
                propertyName: fieldName,
                code: "Ghn.Address.Missing",
                description: "GHN requires carrier address data with district_id and ward_code.");

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("district_id", out var districtEl) ||
                !root.TryGetProperty("ward_code",   out var wardEl))
            {
                return Error.Validation(
                    propertyName: fieldName,
                    code: "Ghn.Address.MissingFields",
                    description: "GHN carrier address data must contain district_id and ward_code.");
            }

            var districtId = districtEl.GetInt32();
            var wardCode   = wardEl.GetString() ?? "";

            if (districtId == 0 || string.IsNullOrWhiteSpace(wardCode))
                return Error.Validation(
                    propertyName: fieldName,
                    code: "Ghn.Address.InvalidValues",
                    description: "GHN district_id must be non-zero and ward_code must be non-empty.");

            return (districtId, wardCode);
        }
        catch (Exception ex)
        {
            return Error.Unexpected(
                code: "Ghn.Address.ParseError",
                description: $"Failed to parse GHN carrier address data: {ex.Message}");
        }
    }

    private static int ParsePaymentTypeId(string? raw) =>
        raw == "2" ? 2 : 1;  // Default 1 = shop pays

    private static int ParseServiceTypeId(string? extraDataJson)
    {
        if (string.IsNullOrWhiteSpace(extraDataJson)) return 2;
        try
        {
            using var doc = JsonDocument.Parse(extraDataJson);
            if (doc.RootElement.TryGetProperty("service_type_id", out var el))
                return el.GetInt32();
        }
        catch { /* ignore — use default */ }
        return 2;
    }

    /// <summary>
    /// Maps GHN string status to our NormalizedTrackingStatus.
    ///
    /// GHN statuses (as documented):
    ///   ready_to_pick, picking, picked, storing, transporting,
    ///   delivering, delivered, delivery_fail, waiting_to_return,
    ///   return, returned, cancel, exception, damage, lost
    /// </summary>
    private static NormalizedTrackingStatus NormalizeGhnStatus(string ghnStatus) =>
        ghnStatus.ToLowerInvariant() switch
        {
            "ready_to_pick"     => NormalizedTrackingStatus.Confirmed,
            "picking"           => NormalizedTrackingStatus.InTransit,   
            "picked"            => NormalizedTrackingStatus.PickedUp,
            "storing"           => NormalizedTrackingStatus.InTransit,
            "transporting"      => NormalizedTrackingStatus.InTransit,
            "delivering"        => NormalizedTrackingStatus.Delivering,   
            "delivered"         => NormalizedTrackingStatus.Delivered,
            "delivery_fail"     => NormalizedTrackingStatus.Failed,      
            "waiting_to_return" => NormalizedTrackingStatus.Returning,
            "return"            => NormalizedTrackingStatus.Returning,
            "returned"          => NormalizedTrackingStatus.Returned,
            "cancel"            => NormalizedTrackingStatus.Cancelled,
            "exception"         => NormalizedTrackingStatus.Failed,
            "damage"            => NormalizedTrackingStatus.Failed,
            "lost"              => NormalizedTrackingStatus.Failed,
            _                   => NormalizedTrackingStatus.InTransit
        };

    /// <summary>
    /// Builds an HttpClient with GHN auth headers pre-set.
    /// Token and ShopId are static per config — no OAuth flow.
    /// </summary>
    private HttpClient BuildClient(ShippingProviderConfig config, GhnCredentials creds)
    {
        var http = _httpClientFactory.CreateClient("GhnClient");
        http.BaseAddress = new Uri(config.ApiBaseUrl);
        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("Token",  creds.Token);
        http.DefaultRequestHeaders.Add("ShopId", creds.ShopId.ToString());
        return http;
    }
}