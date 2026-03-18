using System.Security.Cryptography;
using System.Text;

namespace OIO.Infrastructure.Payment.VnPay;

/// <summary>
/// Utility class cho VNPay: tạo chữ ký HMAC-SHA512, build query string, validate signature.
/// </summary>
public static class VnPayHelper
{
    /// <summary>
    /// Tạo chữ ký HMAC-SHA512.
    /// </summary>
    public static string HmacSha512(string key, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Build query string từ SortedDictionary (theo thứ tự alphabet) — KHÔNG bao gồm vnp_SecureHash.
    /// </summary>
    public static string BuildQueryString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;

            if (!first) sb.Append('&');
            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Xác thực chữ ký VNPay trong callback.
    /// </summary>
    public static bool ValidateSignature(IDictionary<string, string> queryParams, string hashSecret)
    {
        if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash))
            return false;

        var sorted = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (key, value) in queryParams)
        {
            // Loại bỏ các trường hash khỏi dữ liệu cần ký
            if (key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(value))
                sorted[key] = value;
        }

        var signData = BuildQueryString(sorted);
        var computedHash = HmacSha512(hashSecret, signData);

        return string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parse query string thành dictionary. Dùng cho VNPay IPN/Return callback.
    /// </summary>
    public static Dictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(queryString)) return result;

        // Loại bỏ dấu '?' ở đầu nếu có
        if (queryString.StartsWith('?'))
            queryString = queryString[1..];

        foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
