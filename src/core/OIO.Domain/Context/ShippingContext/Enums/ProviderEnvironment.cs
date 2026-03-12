using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class ProviderEnvironment : EnumValueObject<ProviderEnvironment>
{
    public static readonly ProviderEnvironment Sandbox = new("sandbox");
    public static readonly ProviderEnvironment Production = new("production");
    private ProviderEnvironment(string id) : base(id) { }
}