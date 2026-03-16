using System.Globalization;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Payment;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Payment.VnPay;

/// <summary>
/// VNPay payment gateway implementation.
/// Tạo URL thanh toán, xử lý IPN callback, hoàn tiền.
/// </summary>
public sealed class VnPayGateway : IPaymentGatewayService
{
    private readonly VnPayConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<VnPayGateway> _logger;

    public string ProviderCode => "vnpay";

    public VnPayGateway(
        IOptions<VnPayConfig> config,
        HttpClient httpClient,
        ILogger<VnPayGateway> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public Result<CreatePaymentUrlResult, Error> CreatePaymentUrl(CreatePaymentUrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(_config.TmnCode))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay TmnCode is not configured.");

        if (string.IsNullOrWhiteSpace(_config.HashSecret))
            return Error.Unavailable("VnPay.NotConfigured", "VNPay HashSecret is not configured.");

        var vnpParams = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"]    = _config.Version,
            ["vnp_Command"]    = "pay",
            ["vnp_TmnCode"]    = _config.TmnCode,
            ["vnp_Amount"]     = (request.Amount * 100).ToString(CultureInfo.InvariantCulture), // VNPay yêu cầu nhân 100
            ["vnp_CurrCode"]   = "VND",
            ["vnp_TxnRef"]     = request.TransactionRef,
            ["vnp_OrderInfo"]  = request.OrderDescription,
            ["vnp_OrderType"]  = MapPurposeToOrderType(request.Purpose),
            ["vnp_Locale"]     = request.Locale,
            ["vnp_ReturnUrl"]  = _config.ReturnUrl,
            ["vnp_IpAddr"]     = request.IpAddress,
            ["vnp_CreateDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"), // GMT+7
        };

        if (!string.IsNullOrWhiteSpace(request.BankCode))
            vnpParams["vnp_BankCode"] = request.BankCode;

        var queryString = VnPayHelper.BuildQueryString(vnpParams);
        var secureHash = VnPayHelper.HmacSha512(_config.HashSecret, queryString);

        var paymentUrl = $"{_config.PaymentUrl}?{queryString}&vnp_SecureHash={secureHash}";

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
        if (!VnPayHelper.ValidateSignature(queryParams, _config.HashSecret))
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

        if (string.IsNullOrWhiteSpace(txnRef) || string.IsNullOrWhiteSpace(responseCode))
        {
            return Error.Validation("QueryParams", "VnPay.MissingFields",
                "VNPay callback is missing required fields (vnp_TxnRef, vnp_ResponseCode).");
        }

        // VNPay amount đã nhân 100
        var amount = long.TryParse(amountStr, out var rawAmount) ? rawAmount / 100 : 0;

        var rawJson = JsonSerializer.Serialize(queryParams);

        _logger.LogInformation(
            "VNPay callback processed: TxnRef={TxnRef}, ResponseCode={ResponseCode}, TransactionStatus={TransactionStatus}",
            txnRef, responseCode, transactionStatus);

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
            ["vnp_Version"] = _config.Version,
            ["vnp_Command"] = "querydr",
            ["vnp_TmnCode"] = _config.TmnCode,
            ["vnp_TxnRef"] = transactionRef,
            ["vnp_OrderInfo"] = $"Query transaction {transactionRef}",
            ["vnp_TransactionDate"] = createdDate,
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = "127.0.0.1", // For background job, we can just use localhost IP
        };

        var signData = string.Join("|",
            requestId, _config.Version, "querydr", _config.TmnCode,
            transactionRef, createdDate, createDate, "127.0.0.1", $"Query transaction {transactionRef}");

        vnpParams["vnp_SecureHash"] = VnPayHelper.HmacSha512(_config.HashSecret, signData);

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(vnpParams),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_config.ApiUrl, jsonContent, ct);
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
            ["vnp_RequestId"]    = requestId,
            ["vnp_Version"]      = _config.Version,
            ["vnp_Command"]      = "refund",
            ["vnp_TmnCode"]      = _config.TmnCode,
            ["vnp_TransactionType"] = "02", // Hoàn tiền toàn phần
            ["vnp_TxnRef"]       = request.OriginalTransactionRef,
            ["vnp_Amount"]       = (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            ["vnp_TransactionNo"] = request.OriginalVnPayTransactionNo,
            ["vnp_TransactionDate"] = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_CreateBy"]     = request.CreatedBy,
            ["vnp_CreateDate"]   = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss"),
            ["vnp_IpAddr"]       = request.IpAddress,
            ["vnp_OrderInfo"]    = request.Reason,
        };

        // Tạo chuỗi ký: requestId|version|command|tmnCode|transactionType|txnRef|amount|transactionNo|transactionDate|createBy|createDate|ipAddr|orderInfo
        var signData = string.Join("|",
            requestId, _config.Version, "refund", _config.TmnCode,
            "02", request.OriginalTransactionRef,
            (request.Amount * 100).ToString(CultureInfo.InvariantCulture),
            request.OriginalVnPayTransactionNo,
            vnpParams["vnp_TransactionDate"],
            request.CreatedBy,
            vnpParams["vnp_CreateDate"],
            request.IpAddress,
            request.Reason);

        vnpParams["vnp_SecureHash"] = VnPayHelper.HmacSha512(_config.HashSecret, signData);

        try
        {
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(vnpParams),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(_config.ApiUrl, jsonContent, ct);
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

    private static string MapPurposeToOrderType(PaymentPurpose purpose) => purpose switch
    {
        PaymentPurpose.AuctionDeposit => "250000", // Thanh toán khác
        PaymentPurpose.OrderPayment   => "200000", // Thanh toán hàng hóa
        PaymentPurpose.AuctionBuyNow  => "200000", // Thanh toán hàng hóa
        PaymentPurpose.WalletTopUp    => "250000", // Thanh toán khác
        _                             => "250000",
    };
}
