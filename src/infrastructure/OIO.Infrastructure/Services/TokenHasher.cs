using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OIO.Domain.Context.UserContext.Services;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Services;

internal sealed class TokenHasher : ITokenHasher
{
    
    private readonly byte[] _key;

    public TokenHasher(IOptionsMonitor<HashingOptions> options)
    {
        _key = Convert.FromBase64String(options.CurrentValue.HmacKeyBase64);
    }

    public string Hash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }

    public bool Verify(string value, string hashedValue)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(hashedValue);

        byte[] expected;
        try
        {
            expected = Convert.FromHexString(hashedValue);
        }
        catch (FormatException)
        {
            return false;
        }

        using var hmac = new HMACSHA256(_key);
        var actual = hmac.ComputeHash(Encoding.UTF8.GetBytes(value));

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}