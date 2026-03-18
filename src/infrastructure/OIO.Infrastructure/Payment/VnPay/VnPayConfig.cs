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
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>URL IPN callback (server-to-server)</summary>
    public string IpnUrl { get; set; } = string.Empty;

    /// <summary>Phiên bản API VNPay</summary>
    public string Version { get; set; } = "2.1.0";
}
