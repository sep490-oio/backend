using CSharpFunctionalExtensions;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.ShippingProviders;

/// <summary>
/// Carrier configuration and credentials.
/// credentials jsonb is encrypted at rest.
/// One row per carrier per environment (sandbox/production).
/// </summary>
public sealed class ShippingProviderConfig : AggregateRoot<ShippingProviderConfigId>
{
    private ShippingProviderConfig() { }

    private ShippingProviderConfig(
        ShippingProviderConfigId id,
        ShippingProviderCode providerCode,
        string displayName,
        ShippingEnvironment environment,
        string apiBaseUrl,
        ShippingCredentials credentials,
        string? webhookSecret,
        string pickName,
        string pickPhone,
        string pickAddress,
        string pickWard,
        string pickDistrict,
        string pickProvince,
        CarrierAddressData? pickCarrierAddressData,
        bool isDefault,
        DateTime now)
    {
        Id                   = id;
        ProviderCode         = providerCode;
        DisplayName          = displayName;
        Environment          = environment;
        ApiBaseUrl           = apiBaseUrl;
        Credentials          = credentials;
        WebhookSecret        = webhookSecret;
        PickName             = pickName;
        PickPhone            = pickPhone;
        PickAddress          = pickAddress;
        PickWard             = pickWard;
        PickDistrict         = pickDistrict;
        PickProvince         = pickProvince;
        PickCarrierAddressData = pickCarrierAddressData;
        IsActive             = true;
        IsDefault            = isDefault;
        CreatedAt            = now;
    }

    public ShippingProviderCode ProviderCode { get; private set; }
    public string DisplayName { get; private set; }
    public ShippingEnvironment Environment { get; private set; }
    public string ApiBaseUrl { get; private set; }

    /// <summary>
    /// Encrypted at rest. Shape per provider:
    /// GHN:  { "token": "...", "shop_id": 885, "client_id": 500379 }
    /// GHTK: { "token": "...", "x_client_source": "S308157" }
    /// </summary>
    public ShippingCredentials Credentials { get; private set; }

    /// <summary>
    /// For expiring-token carriers (e.g. Viettel Post in the future).
    /// Null for GHN and GHTK which use static tokens.
    /// </summary>
    public string? CachedToken { get; private set; }
    public DateTime? CachedTokenExpiresAt { get; private set; }

    /// <summary>GHTK: hash param on callback URL. Null for GHN.</summary>
    public string? WebhookSecret { get; private set; }

    // Warehouse pickup address
    public string PickName { get; private set; }
    public string PickPhone { get; private set; }
    public string PickAddress { get; private set; }
    public string PickWard { get; private set; }
    public string PickDistrict { get; private set; }
    public string PickProvince { get; private set; }

    /// <summary>GHN: { "district_id": ..., "ward_code": "..." }. Null for GHTK.</summary>
    public CarrierAddressData? PickCarrierAddressData { get; private set; }

    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public static ShippingProviderConfig Create(
        ShippingProviderCode providerCode,
        string displayName,
        ShippingEnvironment environment,
        string apiBaseUrl,
        ShippingCredentials credentials,
        string pickName,
        string pickPhone,
        string pickAddress,
        string pickWard,
        string pickDistrict,
        string pickProvince,
        DateTime now,
        string? webhookSecret = null,
        CarrierAddressData? pickCarrierAddressData = null,
        bool isDefault = false)
        => new(
            ShippingProviderConfigId.From(Guid.CreateVersion7()),
            providerCode,
            displayName,
            environment,
            apiBaseUrl,
            credentials,
            webhookSecret,
            pickName,
            pickPhone,
            pickAddress,
            pickWard,
            pickDistrict,
            pickProvince,
            pickCarrierAddressData,
            isDefault,
            now);

    public void UpdateCredentials(ShippingCredentials credentials, DateTime now)
    {
        Credentials = credentials;
        ModifiedAt  = now;
    }

    /// <summary>For expiring-token carriers — cache the refreshed token.</summary>
    public void UpdateCachedToken(string token, DateTime expiresAt, DateTime now)
    {
        CachedToken           = token;
        CachedTokenExpiresAt  = expiresAt;
        ModifiedAt            = now;
    }

    public bool IsCachedTokenValid(DateTime now)
        => CachedToken is not null &&
           CachedTokenExpiresAt.HasValue &&
           CachedTokenExpiresAt.Value > now;

    public void Activate(DateTime now)
    {
        IsActive   = true;
        ModifiedAt = now;
    }

    public void Deactivate(DateTime now)
    {
        IsActive   = false;
        IsDefault  = false;
        ModifiedAt = now;
    }

    public UnitResult<e> SetAsDefault(DateTime now)
    {
        if (!IsActive)
            return WarehouseErrors.ShippingProvider.ProviderInactive;

        IsDefault  = true;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }
    public void UpdateDetails(
        string displayName,
        string apiBaseUrl,
        string pickName,
        string pickPhone,
        string pickAddress,
        string pickWard,
        string pickDistrict,
        string pickProvince,
        string? webhookSecret,
        CarrierAddressData? pickCarrierAddressData,
        DateTime now)
    {
        DisplayName            = displayName;
        ApiBaseUrl             = apiBaseUrl;
        PickName               = pickName;
        PickPhone              = pickPhone;
        PickAddress            = pickAddress;
        PickWard               = pickWard;
        PickDistrict           = pickDistrict;
        PickProvince           = pickProvince;
        WebhookSecret          = webhookSecret;
        PickCarrierAddressData = pickCarrierAddressData;
        ModifiedAt             = now;
    }
}