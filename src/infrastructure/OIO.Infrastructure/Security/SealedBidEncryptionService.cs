using System.Globalization;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.DataProtection;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Security;

internal sealed class SealedBidEncryptionService : ISealedBidEncryptionService
{
    private const string Purpose = "auction:sealed-bids";

    private readonly IDataProtector _protector;

    public SealedBidEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
    }

    public string Encrypt(decimal amount)
    {
        return _protector.Protect(amount.ToString(CultureInfo.InvariantCulture));
    }

    public Result<decimal, Error> Decrypt(string encryptedAmount)
    {
        try
        {
            var plaintext = _protector.Unprotect(encryptedAmount);

            return decimal.TryParse(
                plaintext,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount)
                ? amount
                : Error.Validation(
                    "Amount",
                    "SealedBid.InvalidCiphertext",
                    "Failed to parse decrypted sealed bid amount.");
        }
        catch
        {
            return Error.Validation(
                "Amount",
                "SealedBid.InvalidCiphertext",
                "Failed to decrypt sealed bid amount.");
        }
    }
}
