using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Infrastructure.Shipping;

/// <summary>
/// Resolves the correct IShippingProvider by provider code string.
/// All registered providers are injected via IEnumerable (Scrutor/DI scans them).
/// </summary>
internal sealed class ShippingProviderSelector : IShippingProviderSelector
{
    private readonly IReadOnlyDictionary<string, IShippingProvider> _providers;

    public ShippingProviderSelector(IEnumerable<IShippingProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderCode, StringComparer.OrdinalIgnoreCase);
    }

    public Result<IShippingProvider, Error> Select(string providerCode)
    {
        if (_providers.TryGetValue(providerCode, out var provider))
            return Result.Success<IShippingProvider, Error>(provider);

        return Error.NotFound(
            code: "ShippingProvider.NotFound",
            description: $"No shipping provider registered for code '{providerCode}'. Registered: {string.Join(", ", _providers.Keys)}.");
    }
}