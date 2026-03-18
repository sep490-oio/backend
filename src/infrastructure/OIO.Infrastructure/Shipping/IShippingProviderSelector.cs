using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Shipping;

public interface IShippingProviderSelector
{
    /// <summary>Returns the provider matching providerCode, or Error.NotFound if not registered.</summary>
    Result<IShippingProvider, Error> Select(string providerCode);
}