using System.Security.Cryptography;
using System.Text;
using OIO.Application.Abstractions.Security;

namespace OIO.Infrastructure.Security;

internal sealed class SecureTokenGenerator : ISecureTokenGenerator
{
    public string GenerateToken(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(bytes);
    }
}