using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace OIO.Infrastructure.Payment.VnPay;

/// <summary>
/// Utility class cho VNPay: tạo chữ ký HMAC-SHA512, build query string, validate signature.
/// </summary>
public static class VnPayHelper
{
    private const int MaxOrderInfoLength = 255;

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
    /// Build query string từ SortedDictionary (theo thứ tự alphabet)
    /// theo semantics application/x-www-form-urlencoded, không bao gồm vnp_SecureHash.
    /// </summary>
    public static string BuildQueryString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        var first = true;

        foreach (var (key, value) in data)
        {
            if (string.IsNullOrEmpty(value)) continue;

            if (!first) sb.Append('&');
            sb.Append(EncodeFormComponent(key));
            sb.Append('=');
            sb.Append(EncodeFormComponent(value));
            first = false;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Chuẩn hóa OrderInfo theo quy định VNPay:
    /// không dấu, ASCII, chỉ giữ lại [a-zA-Z0-9 space . , : -], gộp space thừa, tối đa 255 ký tự.
    /// </summary>
    public static string NormalizeOrderInfo(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decomposed = value
            .Replace('Đ', 'D')
            .Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            if (IsAllowedOrderInfoCharacter(ch))
            {
                sb.Append(ch);
                continue;
            }

            sb.Append(' ');
        }

        var normalized = CollapseWhitespace(sb.ToString());
        if (normalized.Length > MaxOrderInfoLength)
            normalized = normalized[..MaxOrderInfoLength].TrimEnd();

        return normalized;
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

        if (queryString.StartsWith('?'))
            queryString = queryString[1..];

        foreach (var pair in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = DecodeFormComponent(parts[0]);
            var value = parts.Length > 1 ? DecodeFormComponent(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static bool IsAllowedOrderInfoCharacter(char ch)
        => ch <= 127 &&
           (char.IsLetterOrDigit(ch) || ch is ' ' or '.' or ',' or ':' or '-');

    private static string CollapseWhitespace(string value)
    {
        var sb = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (previousWasWhitespace)
                    continue;

                sb.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            sb.Append(ch);
            previousWasWhitespace = false;
        }

        return sb.ToString();
    }

    private static string EncodeFormComponent(string value)
        => Uri.EscapeDataString(value).Replace("%20", "+");

    private static string DecodeFormComponent(string value)
        => Uri.UnescapeDataString(value.Replace("+", " "));
}
