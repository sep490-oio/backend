# Shipping Provider Config

Configuration and credentials for carrier integrations (GHN, GHTK, external).

## Entity: ShippingProviderConfig

| Property | Type | Description |
|---|---|---|
| `Id` | `ShippingProviderConfigId` | GUID v7 |
| `ProviderCode` | `ShippingProviderCode` | `ghn`, `ghtk`, or `external` |
| `DisplayName` | `string` | Human-readable provider label |
| `Environment` | `ShippingEnvironment` | `sandbox` or `production` |
| `ApiBaseUrl` | `string` | GHN sandbox: `https://dev-online-gateway.ghn.vn`, prod: `https://online-gateway.ghn.vn` |
| `Credentials` | `ShippingCredentials` | Encrypted JSON blob (see below) |
| `CachedToken` | `string?` | For expiring-token carriers (not used by GHN/GHTK) |
| `CachedTokenExpiresAt` | `DateTime?` | Expiry time for cached token |
| `WebhookSecret` | `string?` | GHTK: hash param on callback URL. Null for GHN |
| `PickName` | `string` | Warehouse pickup contact name |
| `PickPhone` | `string` | Warehouse pickup phone |
| `PickAddress` | `string` | Warehouse pickup street address |
| `PickWard` | `string` | Warehouse pickup ward |
| `PickDistrict` | `string` | Warehouse pickup district |
| `PickProvince` | `string` | Warehouse pickup province |
| `PickCarrierAddressData` | `CarrierAddressData?` | GHN: `{"district_id": ..., "ward_code": "..."}`. Null for GHTK |
| `IsActive` | `bool` | Whether this config is currently enabled |
| `IsDefault` | `bool` | Whether this is the default provider (used when no providerCode specified) |
| `CreatedAt` | `DateTime` | Creation timestamp |
| `ModifiedAt` | `DateTime?` | Last modification timestamp |

One row per carrier per environment (sandbox/production). Credentials JSON is encrypted at rest.

## ShippingCredentials

A value object wrapping a raw JSON string. Created via `ShippingCredentials.From(rawJson)`.

### GHN Credentials Format

```json
{
  "token": "672e7794-...",
  "shop_id": 199494,
  "client_id": 2511126
}
```

Deserialized to `GhnCredentials`:

| Field | JSON Key | Type | Required |
|---|---|---|---|
| `Token` | `token` | `string` | Yes -- must be non-empty |
| `ShopId` | `shop_id` | `int` | Yes -- must be non-zero |
| `ClientId` | `client_id` | `int` | No |

### GHTK Credentials Format

```json
{
  "token": "...",
  "x_client_source": "S308157"
}
```

## Provider Codes

| Code | Class | Description |
|---|---|---|
| `ghn` | `ShippingProviderCode.Ghn` | Giao Hang Nhanh |
| `ghtk` | `ShippingProviderCode.Ghtk` | Giao Hang Tiet Kiem |
| `external` | `ShippingProviderCode.External` | External/manual provider |

## Update Shipping Provider Config

### Endpoint

```
PUT /api/warehouse/shipping-provider-configs/{configId}
Permission: Warehouse.ManageLocations
```

### Request Body

| Field | Type | Required | Description |
|---|---|---|---|
| `DisplayName` | `string` | Yes | Human-readable name |
| `ApiBaseUrl` | `string` | Yes | API base URL |
| `PickName` | `string` | Yes | Warehouse pickup contact name |
| `PickPhone` | `string` | Yes | Warehouse pickup phone |
| `PickAddress` | `string` | Yes | Warehouse pickup address |
| `PickWard` | `string` | Yes | Warehouse pickup ward |
| `PickDistrict` | `string` | Yes | Warehouse pickup district |
| `PickProvince` | `string` | Yes | Warehouse pickup province |
| `PickCarrierAddressDataJson` | `string?` | No | GHN address IDs JSON |
| `WebhookSecret` | `string?` | No | GHTK webhook hash |
| `CredentialsJson` | `string?` | No | Null = don't update credentials |

### Handler Logic (UpdateShippingProviderConfigCommandHandler)

1. Load `ShippingProviderConfig` by `ConfigId`
2. Parse `PickCarrierAddressDataJson` to `CarrierAddressData` if provided
3. Call `config.UpdateDetails(...)` -- updates display name, API URL, pickup address, webhook secret, and carrier address data
4. If `CredentialsJson` is not null, call `config.UpdateCredentials(ShippingCredentials.From(json), now)` -- updates encrypted credentials
5. SaveChanges

### Response: 204 No Content

## Provider Selection

When booking a shipment, the handler resolves the provider config:

1. **Explicit provider**: if `ProviderCode` is specified in the command, look up the active config matching that code:
   ```csharp
   config = await dbContext.Set<ShippingProviderConfig>()
       .FirstOrDefaultAsync(c => c.ProviderCode == providerCode && c.IsActive);
   ```

2. **Default provider**: if `ProviderCode` is null, look up the default active provider:
   ```csharp
   config = await dbContext.Set<ShippingProviderConfig>()
       .FirstOrDefaultAsync(c => c.IsDefault && c.IsActive);
   ```

3. Error `ShippingProvider.NoDefault` if no default is configured.

## Auth Headers (GHN)

GHN uses static token authentication. The `GhnShippingProvider.BuildClient()` method sets:

```
Token: {credentials.Token}
ShopId: {credentials.ShopId}
```

These are added as HTTP headers on every request. No OAuth flow or token refresh is needed.

## Config Lifecycle Methods

| Method | Description |
|---|---|
| `UpdateCredentials(credentials, now)` | Replace encrypted credentials JSON |
| `UpdateCachedToken(token, expiresAt, now)` | Cache a refreshed token (for future carriers) |
| `IsCachedTokenValid(now)` | Check if cached token exists and is not expired |
| `Activate(now)` | Set `IsActive = true` |
| `Deactivate(now)` | Set `IsActive = false`, `IsDefault = false` |
| `SetAsDefault(now)` | Set `IsDefault = true` (must be active, else `ShippingProvider.Inactive`) |
| `UpdateDetails(...)` | Update display name, API URL, pickup address, webhook secret |

## Error Codes

| Code | HTTP | Condition |
|---|---|---|
| `ShippingProvider.NotFound` | 404 | No active config for given provider code or config ID |
| `ShippingProvider.NoDefault` | 404 | No default shipping provider configured |
| `ShippingProvider.Inactive` | 409 | Cannot set as default when inactive |
| `Ghn.Credentials.Invalid` | 500 | Token is empty or shop_id is 0 |
| `Ghn.Credentials.ParseError` | 500 | Failed to deserialize credentials JSON |
