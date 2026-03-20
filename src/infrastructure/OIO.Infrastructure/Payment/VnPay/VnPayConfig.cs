namespace OIO.Infrastructure.Payment.VnPay;

/// <summary>
/// Cấu hình kết nối VNPay — bind từ appsettings section "VnPay".
/// </summary>
public sealed class VnPayConfig
{
    public const string SectionName = "VnPay";

    /// <summary>Mã website tại VNPay (Terminal ID)</summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>Chuỗi bí mật dùng ký HMAC-SHA512</summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>URL trang thanh toán VNPay (sandbox hoặc production)</summary>
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    /// <summary>URL API VNPay (query, refund)</summary>
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";

    /// <summary>URL return sau khi thanh toán (frontend redirect)</summary>
    public string ReturnPath { get; set; } = string.Empty;

    /// <summary>URL IPN callback (server-to-server)</summary>
    public string IpnPath { get; set; } = string.Empty;

    /// <summary>Phiên bản API VNPay</summary>
    public string Version { get; set; } = "2.1.0";

    // ── Token Payment URLs ──────────────────────────────────────────────────

    /// <summary>URL tạo token không thanh toán (vnp_command = token_create)</summary>
    public string TokenCreateUrl { get; set; } = "https://sandbox.vnpayment.vn/token_ui/create-token.html";

    /// <summary>URL thanh toán + tạo token cùng lúc (vnp_command = pay_and_create)</summary>
    public string PayAndCreateUrl { get; set; } = "https://sandbox.vnpayment.vn/token_ui/pay-create-token.html";

    /// <summary>URL thanh toán bằng token đã lưu (vnp_command = token_pay)</summary>
    public string TokenPayUrl { get; set; } = "https://sandbox.vnpayment.vn/token_ui/payment-token.html";

    /// <summary>URL xóa token (vnp_command = token_remove)</summary>
    public string TokenRemoveUrl { get; set; } = "https://sandbox.vnpayment.vn/token_ui/remove-token.html";
}
