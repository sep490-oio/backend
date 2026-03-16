using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Services;

public interface ISealedBidEncryptionService
{
    string Encrypt(decimal amount);

    Result<decimal, Error> Decrypt(string encryptedAmount);
}
