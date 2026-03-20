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
        if (string.IsNullOrWhiteSpace(_vnPayConfig.CurrentValue.TmnCode))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay TmnCode is not configured.");

        if (string.IsNullOrWhiteSpace(_vnPayConfig.CurrentValue.HashSecret))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay HashSecret is not configured.");

        var createDate = _clock.UtcNow.AddHours(7);
        var expireDate = createDate.AddMinutes(15);
        var orderInfo = VnPayHelper.NormalizeOrderInfo(request.OrderDescription);

        if (string.IsNullOrWhiteSpace(orderInfo))
            orderInfo = VnPayHelper.NormalizeOrderInfo($"Thanh toan giao dich {request.TransactionRef}");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_Amount"] = (request.Amount * 100).ToString(CultureInfo.InvariantCulture), // VNPay yêu cầu nhân 100
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.TransactionRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = "250000",
            ["vnp_Locale"] = request.Locale,
            ["vnp_ReturnUrl"] = $"{_appInfo.BeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"), // GMT+7
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"), // GMT+7 + 15 phút
        };

        if (!string.IsNullOrWhiteSpace(request.BankCode))
            vnpParams["vnp_BankCode"] = request.BankCode;

        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, queryString);

        var paymentUrl = $"{_vnPayConfig.CurrentValue.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";

        _logger.LogInformation(
            "VNPay payment URL created for TxnRef={TxnRef}, Amount={Amount}",
            request.TransactionRef, request.Amount);

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

        _logger.LogInformation(
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

            _logger.LogInformation(
                "VNPay query response for TxnRef={TxnRef}: {Response}",
                transactionRef, responseBody);

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

            _logger.LogInformation(
                "VNPay refund response for TxnRef={TxnRef}: {Response}",
                request.OriginalTransactionRef, responseBody);

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

        var createDate = _clock.UtcNow.AddHours(7);
        var expireDate = createDate.AddMinutes(15);
        var orderInfo = VnPayHelper.NormalizeOrderInfo(request.OrderDescription);

        if (string.IsNullOrWhiteSpace(orderInfo))
            orderInfo = VnPayHelper.NormalizeOrderInfo($"Thanh toan giao dich {request.TransactionRef}");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "pay_and_create",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_Amount"] = (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.TransactionRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_Locale"] = request.Locale,
            ["vnp_ReturnUrl"] = $"{_appInfo.BeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
            ["vnp_AppUserId"] = request.AppUserId,
            ["vnp_StoreToken"] = "1",
        };

        if (!string.IsNullOrWhiteSpace(request.CardType))
            vnpParams["vnp_CardType"] = request.CardType;

        return BuildTokenUrl(_vnPayConfig.CurrentValue.PayAndCreateUrl, vnpParams, request.TransactionRef, "pay_and_create");
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreateTokenPayUrl(TokenPaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        var createDate = _clock.UtcNow.AddHours(7);
        var expireDate = createDate.AddMinutes(15);
        var orderInfo = VnPayHelper.NormalizeOrderInfo(request.OrderDescription);

        if (string.IsNullOrWhiteSpace(orderInfo))
            orderInfo = VnPayHelper.NormalizeOrderInfo($"Thanh toan giao dich {request.TransactionRef}");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "token_pay",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_Amount"] = (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.TransactionRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_Locale"] = request.Locale,
            ["vnp_ReturnUrl"] = $"{_appInfo.BeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss"),
            ["vnp_AppUserId"] = request.AppUserId,
            ["vnp_Token"] = request.Token,
        };

        return BuildTokenUrl(_vnPayConfig.CurrentValue.TokenPayUrl, vnpParams, request.TransactionRef, "token_pay");
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreateTokenOnlyUrl(CreateTokenPaymentUrlRequest request)
    {
        var configCheck = EnsureConfigured();
        if (configCheck.IsFailure) return configCheck.Error;

        var createDate = _clock.UtcNow.AddHours(7);
        var orderInfo = VnPayHelper.NormalizeOrderInfo(request.OrderDescription);

        if (string.IsNullOrWhiteSpace(orderInfo))
            orderInfo = VnPayHelper.NormalizeOrderInfo($"Lien ket the {request.TransactionRef}");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayConfig.CurrentValue.Version,
            ["vnp_Command"] = "token_create",
            ["vnp_TmnCode"] = _vnPayConfig.CurrentValue.TmnCode,
            ["vnp_TxnRef"] = request.TransactionRef,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_Locale"] = request.Locale,
            ["vnp_ReturnUrl"] = $"{_appInfo.BeUrl}{_vnPayConfig.CurrentValue.ReturnPath}",
            ["vnp_IpAddr"] = request.IpAddress,
            ["vnp_CreateDate"] = createDate.ToString("yyyyMMddHHmmss"),
            ["vnp_AppUserId"] = request.AppUserId,
        };

        if (!string.IsNullOrWhiteSpace(request.CardType))
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

            _logger.LogInformation(
                "VNPay token remove response for AppUserId={AppUserId}: {Response}",
                request.AppUserId, responseBody);

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

    private Result<CreatePaymentUrlResult, Error> BuildTokenUrl(
        string baseUrl,
        SortedDictionary<string, string> vnpParams,
        string transactionRef,
        string command)
    {
        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_vnPayConfig.CurrentValue.HashSecret, queryString);

        var paymentUrl = $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";

        _logger.LogInformation(
            "VNPay {Command} URL created for TxnRef={TxnRef}",
            command, transactionRef);

        return new CreatePaymentUrlResult
        {
            PaymentUrl = paymentUrl,
            TransactionRef = transactionRef,
        };
    }
}
