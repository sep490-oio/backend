using OIO.Domain.Context.ShippingContext.Enums;
using OIO.Domain.Context.ShippingContext.ValueObjects;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class ShippingProviderConfig : AggregateRoot<ShippingProviderConfigId>, IAuditableEntity
{
    public string ProviderCode { get; private set; }      // ghn | ghtk
    public string DisplayName { get; private set; }
    public ProviderEnvironment Environment { get; private set; }
    public string ApiBaseUrl { get; private set; }
    public string Credentials { get; private set; }        // jsonb
    public string? CachedToken { get; private set; }
    public DateTime? CachedTokenExpiresAt { get; private set; }
    public string? WebhookSecret { get; private set; }

    // Pick address (warehouse/sender)
    public PickAddress PickAddress { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    private ShippingProviderConfig() { }
}