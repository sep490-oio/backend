using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Payment.VnPay;

/// <summary>
/// VNPay payment gateway implementation.
/// Tạo URL thanh toán, xử lý IPN callback, hoàn tiền.
/// </summary>
public sealed class VnPayGateway : IPaymentGatewayService
{
    private readonly IOptionsMonitor<VnPayConfig> _vnPayConfig;
    private readonly HttpClient _httpClient;
    private readonly IAppInfo _appInfo;
    private readonly IClock _clock;
    private readonly ILogger<VnPayGateway> _logger;

    public string ProviderCode => "vnpay";

    public VnPayGateway(
        IOptionsMonitor<VnPayConfig> vnPayConfig,
        IAppInfo appInf,
        HttpClient httpClient,
        IClock clock,
        ILogger<VnPayGateway> logger)
    {
        _vnPayConfig = vnPayConfig;
        _appInfo = appInf;
        _httpClient = httpClient;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreatePaymentUrl(CreatePaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        var vnpParams = BuildGatewayPayParams(
            command: "pay",
            transactionRef: request.TransactionRef,
            orderInfo: request.OrderDescription,
            locale: request.Locale,
            ipAddress: request.IpAddress,
            amount: request.Amount);

        if (!string.IsNullOrWhiteSpace(request.BankCode))
            vnpParams["vnp_BankCode"] = request.BankCode;

        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, queryString);
        var paymentUrl = $"{_vnPayConfig.CurrentValue.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";

        LogUrlCreated("pay", request.TransactionRef, _vnPayConfig.CurrentValue.PaymentUrl, vnpParams);

        return new CreatePaymentUrlResult
        {
            PaymentUrl = paymentUrl,
            TransactionRef = request.TransactionRef,
        };
    }

    /// <inheritdoc />
    public Result<PaymentCallbackResult, Error> ProcessCallback(IDictionary<string, string> queryParams)
    {
        // 1. Validate signature
        if (!VnPayHelper.ValidateSignature(queryParams, _vnPayConfig.CurrentValue.HashSecret))
        {
            _logger.LogWarning("VNPay callback signature validation failed.");
            return Error.Unauthorized("VnPay.InvalidSignature", "VNPay callback signature is invalid.");
        }

        // 2. Parse response
        queryParams.TryGetValue("vnp_TxnRef", out var txnRef);
        queryParams.TryGetValue("vnp_TransactionNo", out var vnpTransactionNo);
        queryParams.TryGetValue("vnp_Amount", out var amountStr);
        queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
        queryParams.TryGetValue("vnp_TransactionStatus", out var transactionStatus);
        queryParams.TryGetValue("vnp_BankCode", out var bankCode);
        queryParams.TryGetValue("vnp_CardType", out var cardType);
        queryParams.TryGetValue("vnp_PayDate", out var payDate);

        // Token fields (trả về từ pay_and_create / token_create / token_pay)
        queryParams.TryGetValue("vnp_Token", out var vnpToken);
        if (string.IsNullOrWhiteSpace(vnpToken))
            queryParams.TryGetValue("vnp_token", out vnpToken); // case fallback

        queryParams.TryGetValue("vnp_CardNumber", out var cardNumber);
        if (string.IsNullOrWhiteSpace(cardNumber))
            queryParams.TryGetValue("vnp_card_number", out cardNumber);

        if (string.IsNullOrWhiteSpace(txnRef) || string.IsNullOrWhiteSpace(responseCode))
        {
            return Error.Validation("QueryParams", "VnPay.MissingFields",
                "VNPay callback is missing required fields (vnp_TxnRef, vnp_ResponseCode).");
        }

        // VNPay amount đã nhân 100
        var amount = long.TryParse(amountStr, out var rawAmount) ? rawAmount / 100 : 0;

        var rawJson = JsonSerializer.Serialize(queryParams);

        _logger.LogDebug(
            "VNPay callback processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, TransactionStatus={TransactionStatus}, HasToken={HasToken}",
            txnRef, responseCode, transactionStatus, !string.IsNullOrWhiteSpace(vnpToken));

        return new PaymentCallbackResult
        {
            TransactionRef = txnRef,
            VnPayTransactionNo = vnpTransactionNo ?? string.Empty,
            Amount = amount,
            ResponseCode = responseCode,
            TransactionStatus = transactionStatus ?? string.Empty,
            BankCode = bankCode,
            CardType = cardType,
            PayDate = payDate,
            VnPayToken = vnpToken,
            MaskedCardNumber = cardNumber,
            RawResponseJson = rawJson,
        };
    }

    /// <inheritdoc />
    public async Task<Result<QueryTransactionResult, Error>> QueryTransactionAsync(string transactionRef, string createdDate, CancellationToken ct = default)
    {
        var requestId = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..8];
        var createDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "querydr",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TxnRef"] = transactionRef,
            ["vnp_OrderInfo"] = $"Query transaction {transactionRef}",
            ["vnp_TransactionDate"] = createdDate,
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = "127.0.0.1", // For background job, we can just use localhost IP
        };

        var signData = string.Join("|",
            requestId, _vnPayConfig.CurrentValue.Version, "querydr", _vnPayConfig.CurrentValue.TmnCode,
            transactionRef, createdDate, createDate, "127.0.0.1", $"Query transaction {transactionRef}");

        vnpParams["vnp_SecureHash"] = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, signData);

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(vnpParams),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_vnPayConfig.CurrentValue.ApiUrl, jsonContent, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogDebug(
                "VNPay query completed for TxnRef={TxnRef} with HTTP {StatusCode}",
                transactionRef, (int)response.StatusCode);

            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var respCode = root.TryGetProperty("vnp_ResponseCode", out var codeEl) ? codeEl.GetString() ?? "99" : "99";
            var txnStatus = root.TryGetProperty("vnp_TransactionStatus", out var statusEl) ? statusEl.GetString() ?? "" : "";
            var amount = root.TryGetProperty("vnp_Amount", out var amountEl) ? long.Parse(amountEl.GetString() ?? "0") / 100 : 0;

            return new QueryTransactionResult
            {
                IsSuccess = respCode == "00" && txnStatus == "00",
                TransactionRef = transactionRef,
                ResponseCode = respCode,
                TransactionStatus = txnStatus,
                Amount = amount,
                RawResponseJson = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay query failed for TxnRef={TxnRef}", transactionRef);
            return Error.Unavailable("VnPay.QueryFailed", $"VNPay query request failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<Result<RefundResult, Error>> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        var requestId = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..8];

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "refund",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TransactionType"] = "02", // Hoàn tiền toàn phần
            ["vnp_TxnRef"] = request.OriginalTransactionRef,
            ["vnp_Amount"] = (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_TransactionNo"] = request.OriginalVnPayTransactionNo,
            ["vnp_TransactionDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_CreateBy"] = request.CreatedBy,
            ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_OrderInfo"] = request.Reason,
        };

        // Tạo chuỗi ký: requestId|version|command|tmnCode|transactionType|txnRef|amount|transactionNo|transactionDate|createBy|createDate|ipAddr|orderInfo
        var signData = string.Join("|",
            requestId, _vnPayConfig.CurrentValue.Version, "refund", _vnPayConfig.CurrentValue.TmnCode,
            "02", request.OriginalTransactionRef,
            (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            request.OriginalVnPayTransactionNo,
            vnpParams["vnp_TransactionDate"],
            request.CreatedBy,
            vnpParams["vnp_CreateDate"],
            request.IpAddress,
            request.Reason);

        vnpParams["vnp_SecureHash"] = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, signData);

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(vnpParams),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_vnPayConfig.CurrentValue.ApiUrl, jsonContent, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogDebug(
                "VNPay refund completed for TxnRef={TxnRef} with HTTP {StatusCode}",
                request.OriginalTransactionRef, (int)response.StatusCode);

            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var respCode = root.TryGetProperty("vnp_ResponseCode", out var codeEl)
                ? codeEl.GetString() ?? "99"
                : "99";

            var message = root.TryGetProperty("vnp_Message", out var msgEl)
                ? msgEl.GetString()
                : null;

            return new RefundResult
            {
                IsSuccess = respCode == "00",
                ResponseCode = respCode,
                Message = message,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay refund failed for TxnRef={TxnRef}", request.OriginalTransactionRef);
            return Error.Unavailable("VnPay.RefundFailed", $"VNPay refund request failed: {ex.Message}");
        }
    }

    // ── Token Payment Methods ────────────────────────────────────────────────

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreatePayAndCreateTokenUrl(CreateTokenPaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        if (string.IsNullOrWhiteSpace(request.CardType))
            return Error.Validation("CardType", "VnPay.CardTypeRequired",
                "Card type is required for pay_and_create flow.");

        var vnpParams = BuildTokenParams(
            command: "pay_and_create",
            transactionRef: request.TransactionRef,
            txnDesc: request.OrderDescription,
            appUserId: request.AppUserId,
            locale: request.Locale,
            ipAddress: request.IpAddress,
            amount: request.Amount);

        vnpParams["vnp_StoreToken"] = "1";
        vnpParams["vnp_CardType"] = request.CardType;

        return BuildTokenUrl(_vnPayConfig.CurrentValue.PayAndCreateUrl, vnpParams, request.TransactionRef, "pay_and_create");
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreateTokenPayUrl(TokenPaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        if (string.IsNullOrWhiteSpace(request.Token))
            return Error.Validation("Token", "VnPay.TokenRequired",
                "Token is required for token_pay flow.");

        var vnpParams = BuildTokenParams(
            command: "token_pay",
            transactionRef: request.TransactionRef,
            txnDesc: request.OrderDescription,
            appUserId: request.AppUserId,
            locale: request.Locale,
            ipAddress: request.IpAddress,
            amount: request.Amount);

        vnpParams["vnp_Token"] = request.Token;

        return BuildTokenUrl(_vnPayConfig.CurrentValue.TokenPayUrl, vnpParams, request.TransactionRef, "token_pay");
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreateTokenOnlyUrl(CreateTokenPaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        if (string.IsNullOrWhiteSpace(request.CardType))
            return Error.Validation("CardType", "VnPay.CardTypeRequired",
                "Card type is required for token_create flow.");

        var vnpParams = BuildTokenParams(
            command: "token_create",
            transactionRef: request.TransactionRef,
            txnDesc: request.OrderDescription,
            appUserId: request.AppUserId,
            locale: request.Locale,
            ipAddress: request.IpAddress,
            amount: null); // no payment for token creation

        vnpParams["vnp_CardType"] = request.CardType;

        return BuildTokenUrl(_vnPayConfig.CurrentValue.TokenCreateUrl, vnpParams, request.TransactionRef, "token_create");
    }

    /// <inheritdoc />
    public async Task<Result<RefundResult, Error>> RemoveTokenAsync(RemoveTokenRequest request, CancellationToken ct = default)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        var createDate = _clock.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
        var orderInfo = VnPayHelper.NormalizeOrderInfo(request.Description);

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "token_remove",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TxnRef"] = request.TransactionRef,
            ["vnp_AppUserId"] = request.AppUserId,
            ["vnp_Token"] = request.Token,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate,
        };

        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, queryString);
        vnpParams["vnp_SecureHash"] = secureHash;

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(vnpParams),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_vnPayConfig.CurrentValue.TokenRemoveUrl, jsonContent, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            _logger.LogDebug(
                "VNPay token remove completed for AppUserId={AppUserId} with HTTP {StatusCode}",
                request.AppUserId, (int)response.StatusCode);

            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            var respCode = root.TryGetProperty("vnp_response_code", out var codeEl)
                ? codeEl.GetString() ?? "99"
                : "99";

            var message = root.TryGetProperty("vnp_message", out var msgEl)
                ? msgEl.GetString()
                : null;

            return new RefundResult
            {
                IsSuccess = respCode == "00",
                ResponseCode = respCode,
                Message = message,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VNPay token remove failed for AppUserId={AppUserId}", request.AppUserId);
            return Error.Unavailable("VnPay.TokenRemoveFailed", $"VNPay token remove request failed: {ex.Message}");
        }
    }

    // ── Private Helpers ──────────────────────────────────────────────────────

    private UnitResult<Error> EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_vnPayConfig.CurrentValue.TmnCode))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay TmnCode is not configured.");

        if (string.IsNullOrWhiteSpace(_vnPayConfig.CurrentValue.HashSecret))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay HashSecret is not configured.");

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Builds the base VNPay parameter set for the gateway pay flow (CreatePaymentUrl).
    /// Uses gateway pay spec field names (vnp_OrderInfo, vnp_Locale, vnp_ReturnUrl, etc.).
    /// </summary>
    private SortedDictionary<string, string> BuildGatewayPayParams(
        string command,
        string transactionRef,
        string orderInfo,
        string locale,
        string ipAddress,
        decimal? amount = null,
        DateTime? expireDate = null)
    {
        var createDate = _clock.UtcNow.AddHours(7);

        var normalizedInfo = VnPayHelper.NormalizeOrderInfo(orderInfo);
        if (string.IsNullOrWhiteSpace(normalizedInfo))
        {
            var fallback = command == "token_create"
                ? $"Lien ket the {transactionRef}"
                : $"Thanh toan giao dich {transactionRef}";
            normalizedInfo = VnPayHelper.NormalizeOrderInfo(fallback);
        }

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = command,
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TxnRef"] = transactionRef,
            ["vnp_OrderInfo"] = normalizedInfo,
            ["vnp_OrderType"] = "250000",
            ["vnp_Locale"] = locale,
            ["vnp_ReturnUrl"] = $"{_appInfo.FeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_IpAddr"] = VnPayHelper.NormalizeIpAddress(ipAddress),
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
        };

        // Payment flows include Amount, Currency, ExpireDate
        if (amount.HasValue)
        {
            vnpParams["vnp_Amount"] = (amount.Value * 100).ToString(CultureInfo.InvariantCulture);
            vnpParams["vnp_CurrCode"] = "VND";
        }

        if (expireDate.HasValue)
        {
            vnpParams["vnp_ExpireDate"] = expireDate.Value.ToString("yyyyMMddHHmmss");
        }
        else if (amount.HasValue) // payment flows default to 15min expiry
        {
            vnpParams["vnp_ExpireDate"] = createDate.AddMinutes(15).ToString("yyyyMMddHHmmss");
        }

        return vnpParams;
    }

    /// <summary>
    /// Builds VNPay token-spec parameter set for pay_and_create, token_pay, token_create.
    /// Uses token techspec field names (vnp_txn_desc, vnp_return_url, vnp_ip_addr, etc.)
    /// which differ from gateway pay field names.
    /// </summary>
    private SortedDictionary<string, string> BuildTokenParams(
        string command,
        string transactionRef,
        string txnDesc,
        string appUserId,
        string locale,
        string ipAddress,
        decimal? amount = null)
    {
        var createDate = _clock.UtcNow.AddHours(7);

        var normalizedDesc = VnPayHelper.NormalizeOrderInfo(txnDesc);
        if (string.IsNullOrWhiteSpace(normalizedDesc))
        {
            var fallback = command == "token_create"
                ? $"Lien ket the {transactionRef}"
                : $"Thanh toan giao dich {transactionRef}";
            normalizedDesc = VnPayHelper.NormalizeOrderInfo(fallback);
        }

        // Normalize AppUserId: remove dashes from GUID for merchant-safe string
        var safeAppUserId = appUserId.Replace("-", "");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = command,
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TxnRef"] = transactionRef,
            ["vnp_OrderInfo"] = normalizedDesc,
            ["vnp_ReturnUrl"] = $"{_appInfo.FeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_Locale"] = locale,
            ["vnp_IpAddr"] = VnPayHelper.NormalizeIpAddress(ipAddress),
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_AppUserId"] = safeAppUserId,
        };

        // Payment token flows include Amount, Currency, ExpireDate
        if (amount.HasValue && amount.Value > 0)
        {
            vnpParams["vnp_Amount"] = (amount.Value * 100).ToString(CultureInfo.InvariantCulture);
            vnpParams["vnp_CurrCode"] = "VND";
            vnpParams["vnp_ExpireDate"] = createDate.AddMinutes(15).ToString("yyyyMMddHHmmss");
        }

        return vnpParams;
    }

    private Result<CreatePaymentUrlResult, Error> BuildTokenUrl(
        string baseUrl,
        SortedDictionary<string, string> vnpParams,
        string transactionRef,
        string command)
    {
        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, queryString);
        var paymentUrl = $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";

        LogUrlCreated(command, transactionRef, baseUrl, vnpParams);

        return new CreatePaymentUrlResult
        {
            PaymentUrl = paymentUrl,
            TransactionRef = transactionRef,
        };
    }

    private void LogUrlCreated(string command, string transactionRef, string baseUrl, SortedDictionary<string, string> vnpParams)
    {
        var paramKeys = string.Join(", ", vnpParams.Keys);
        _logger.LogDebug(
            "VNPay URL created: Command={Command}, TxnRef={TxnRef}, BaseUrl={BaseUrl}, Params=[{ParamKeys}]",
            command, transactionRef, baseUrl, paramKeys);
    }
}
