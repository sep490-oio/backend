using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Payment;

// ── Application-layer DTOs ────────────────────────────────────────────────────

/// <summary>
/// Mục đích thanh toán, dùng để phân loại luồng xử lý khi VNPay callback.
/// </summary>
public enum PaymentPurpose
{
    /// <summary>Đặt cọc đấu giá (VNPay → Wallet → AuctionDeposit)</summary>
    AuctionDeposit,

    /// <summary>Thanh toán đơn hàng (VNPay → Transaction → Escrow)</summary>
    OrderPayment,

    /// <summary>Thanh toán buy-now đấu giá (VNPay → reservation → sold)</summary>
    AuctionBuyNow,

    /// <summary>Nạp tiền vào ví (VNPay → Wallet)</summary>
    WalletTopUp,
}

public sealed class CreatePaymentUrlRequest
{
    /// <summary>Mã giao dịch nội bộ — dùng làm vnp_TxnRef</summary>
    public required string TransactionRef { get; init; }

    /// <summary>Số tiền (VND, đơn vị đồng)</summary>
    public required long Amount { get; init; }

    /// <summary>Mô tả đơn hàng hiển thị trên VNPay</summary>
    public required string OrderDescription { get; init; }

    /// <summary>Loại thanh toán</summary>
    public required PaymentPurpose Purpose { get; init; }

    /// <summary>IP của người dùng</summary>
    public required string IpAddress { get; init; }

    /// <summary>Locale: vn / en</summary>
    public string Locale { get; init; } = "vn";

    /// <summary>Bank code (tùy chọn, rỗng = VNPay chọn)</summary>
    public string? BankCode { get; init; }
}

public sealed class CreatePaymentUrlResult
{
    public required string PaymentUrl { get; init; }
    public required string TransactionRef { get; init; }
}

public sealed class PaymentCallbackResult
{
    /// <summary>Mã giao dịch nội bộ (vnp_TxnRef)</summary>
    public required string TransactionRef { get; init; }

    /// <summary>Mã giao dịch VNPay (vnp_TransactionNo)</summary>
    public required string VnPayTransactionNo { get; init; }

    /// <summary>Số tiền (VND)</summary>
    public required long Amount { get; init; }

    /// <summary>Mã phản hồi từ VNPay (00 = thành công)</summary>
    public required string ResponseCode { get; init; }

    /// <summary>Mã phản hồi giao dịch (vnp_TransactionStatus)</summary>
    public required string TransactionStatus { get; init; }

    /// <summary>Thanh toán thành công?</summary>
    public bool IsSuccess => ResponseCode == "00" && TransactionStatus == "00";

    /// <summary>Mã ngân hàng</summary>
    public string? BankCode { get; init; }

    /// <summary>Loại thẻ: ATM / QRCODE / …</summary>
    public string? CardType { get; init; }

    /// <summary>Thời gian thanh toán (vnp_PayDate)</summary>
    public string? PayDate { get; init; }

    /// <summary>Raw data từ VNPay dưới dạng JSON</summary>
    public required string RawResponseJson { get; init; }
}

public sealed class RefundRequest
{
    public required string OriginalTransactionRef { get; init; }
    public required string OriginalVnPayTransactionNo { get; init; }
    public required long Amount { get; init; }
    public required string Reason { get; init; }
    public required string IpAddress { get; init; }
    public required string CreatedBy { get; init; }
}

public sealed class RefundResult
{
    public required bool IsSuccess { get; init; }
    public required string ResponseCode { get; init; }
    public string? Message { get; init; }
}

public sealed class QueryTransactionResult
{
    public required bool IsSuccess { get; init; }
    public required string TransactionRef { get; init; }
    public required string ResponseCode { get; init; }
    public required string TransactionStatus { get; init; }
    public required long Amount { get; init; }
    public required string RawResponseJson { get; init; }
}

// ── Interface ─────────────────────────────────────────────────────────────────

/// <summary>
/// Application-layer abstraction cho payment gateway.
/// Implemented bởi VnPayGateway trong Infrastructure.
/// Command handlers inject interface này — không inject trực tiếp implementation.
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>Tên provider ("vnpay")</summary>
    string ProviderCode { get; }

    /// <summary>
    /// Tạo URL thanh toán VNPay để redirect user.
    /// </summary>
    Result<CreatePaymentUrlResult, Error> CreatePaymentUrl(CreatePaymentUrlRequest request);

    /// <summary>
    /// Tìm trạng thái giao dịch trên Gateway.
    /// </summary>
    Task<Result<QueryTransactionResult, Error>> QueryTransactionAsync(
        string transactionRef, string createdDate, CancellationToken ct = default);

    /// <summary>
    /// Xác thực và parse callback (IPN / Return URL) từ VNPay.
    /// </summary>
    Result<PaymentCallbackResult, Error> ProcessCallback(IDictionary<string, string> queryParams);

    /// <summary>
    /// Hoàn tiền qua VNPay.
    /// </summary>
    Task<Result<RefundResult, Error>> RefundAsync(RefundRequest request, CancellationToken ct = default);
}
