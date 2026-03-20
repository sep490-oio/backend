# 09 - VNPay Technical Reference

## Overview

This document covers the low-level technical details of the VNPay integration: URL construction, HMAC-SHA512 signing, callback validation, configuration, and helper utilities.

**Source files**:
- `VnPayGateway` in `OIO.Infrastructure/Payment/VnPay/VnPayGateway.cs`
- `VnPayHelper` in `OIO.Infrastructure/Payment/VnPay/VnPayHelper.cs`
- `VnPayConfig` in `OIO.Infrastructure/Payment/VnPay/VnPayConfig.cs`
- `IPaymentGatewayService` in `OIO.Application/Abstractions/Payment/IPaymentGatewayService.cs`

---

## VnPayConfig (appsettings section `"VnPay"`)

| Property | Type | Default (Sandbox) | Description |
|----------|------|-------------------|-------------|
| `TmnCode` | `string` | _(from config)_ | Terminal ID assigned by VNPay |
| `HashSecret` | `string` | _(from config)_ | Secret key for HMAC-SHA512 signing |
| `PaymentUrl` | `string` | `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` | Standard payment page |
| `ApiUrl` | `string` | `https://sandbox.vnpayment.vn/merchant_webapi/api/transaction` | Query/refund API endpoint |
| `TokenCreateUrl` | `string` | `https://sandbox.vnpayment.vn/token_ui/create-token.html` | Token-only link card page |
| `PayAndCreateUrl` | `string` | `https://sandbox.vnpayment.vn/token_ui/pay-create-token.html` | Pay + create token page |
| `TokenPayUrl` | `string` | `https://sandbox.vnpayment.vn/token_ui/payment-token.html` | Pay with existing token page |
| `TokenRemoveUrl` | `string` | `https://sandbox.vnpayment.vn/token_ui/remove-token.html` | Token removal endpoint |
| `Version` | `string` | `"2.1.0"` | VNPay API version |
| `ReturnPath` | `string` | _(from config)_ | Path appended to `AppInfo.BeUrl` for return URL |
| `IpnPath` | `string` | _(from config)_ | Path for IPN callback |

Return URL is constructed as: `{AppInfo.BeUrl}{VnPayConfig.ReturnPath}`

---

## VNPay Commands Summary

| Command | Method | Base URL | Signing Method | Purpose |
|---------|--------|----------|----------------|---------|
| `pay` | GET (redirect) | `PaymentUrl` | Query-string HMAC | Standard payment |
| `pay_and_create` | GET (redirect) | `PayAndCreateUrl` | Query-string HMAC | Pay + save token |
| `token_create` | GET (redirect) | `TokenCreateUrl` | Query-string HMAC | Link card only (Amount=0) |
| `token_pay` | GET (redirect) | `TokenPayUrl` | Query-string HMAC | Pay with saved token |
| `token_remove` | POST (API) | `TokenRemoveUrl` | Query-string HMAC | Remove saved token |
| `querydr` | POST (API) | `ApiUrl` | Pipe-delimited HMAC | Query transaction status |
| `refund` | POST (API) | `ApiUrl` | Pipe-delimited HMAC | Refund transaction |

---

## URL Construction (Redirect Flows)

All redirect URLs (`pay`, `pay_and_create`, `token_create`, `token_pay`) follow the same pattern:

### 1. Build sorted parameter dictionary

Parameters are stored in a `SortedDictionary<string, string>(StringComparer.Ordinal)` to ensure alphabetical ordering.

### 2. Common parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| `vnp_Version` | `VnPayConfig.Version` | Default `"2.1.0"` |
| `vnp_Command` | Varies | `"pay"` / `"pay_and_create"` / `"token_create"` / `"token_pay"` |
| `vnp_TmnCode` | `VnPayConfig.TmnCode` | |
| `vnp_Amount` | `amount * 100` | **VNPay requires multiplication by 100** |
| `vnp_CurrCode` | `"VND"` | |
| `vnp_TxnRef` | Transaction ref | Max 36 chars |
| `vnp_OrderInfo` | Normalized description | See NormalizeOrderInfo below |
| `vnp_Locale` | `"vn"` (default) | |
| `vnp_ReturnUrl` | `{BeUrl}{ReturnPath}` | |
| `vnp_IpAddr` | Client IP | |
| `vnp_CreateDate` | `yyyyMMddHHmmss` | **GMT+7** (UTC + 7 hours) |
| `vnp_ExpireDate` | `yyyyMMddHHmmss` | **GMT+7 + 15 minutes** |

### 3. Token-specific parameters

| Parameter | Used by | Value |
|-----------|---------|-------|
| `vnp_AppUserId` | `pay_and_create`, `token_create`, `token_pay` | User ID |
| `vnp_StoreToken` | `pay_and_create` | `"1"` |
| `vnp_Token` | `token_pay` | Stored VNPay token |
| `vnp_CardType` | `pay_and_create`, `token_create` | Optional card type filter |
| `vnp_OrderType` | `pay` only | `"250000"` |
| `vnp_BankCode` | `pay` only | Optional bank filter |

### 4. Build query string

```csharp
VnPayHelper.BuildQueryString(vnpParams)
```

Iterates the sorted dictionary, URL-encodes keys and values using `Uri.EscapeDataString` (with `%20` replaced by `+`), joins with `&`. Empty values are skipped.

### 5. Sign and append hash

```csharp
var secureHash = VnPayHelper.HmacSha512(HashSecret, queryString);
var paymentUrl = $"{baseUrl}?{queryString}&vnp_SecureHash={secureHash}";
```

---

## HMAC-SHA512 Signing

```csharp
public static string HmacSha512(string key, string data)
```

- Converts `key` and `data` to UTF-8 bytes
- Computes HMAC-SHA512
- Returns lowercase hex string (no dashes)

### Two signing modes

**Query-string signing** (redirect URLs, token_remove):
- Input: the full query string built from sorted parameters
- Used for: `pay`, `pay_and_create`, `token_create`, `token_pay`, `token_remove`

**Pipe-delimited signing** (API calls):
- Input: specific fields joined with `|` in a fixed order
- Used for: `querydr`, `refund`
- Each command has its own field order (see [06-refund.md](06-refund.md) and [08-reconciliation.md](08-reconciliation.md))

---

## Callback Signature Validation

`VnPayHelper.ValidateSignature(queryParams, hashSecret)`:

1. Extract `vnp_SecureHash` from query params
2. Remove `vnp_SecureHash` and `vnp_SecureHashType` from the dictionary
3. Sort remaining params into `SortedDictionary<string, string>(StringComparer.Ordinal)`
4. Build query string from sorted params
5. Compute `HmacSha512(hashSecret, queryString)`
6. Compare computed hash with received hash (case-insensitive)

---

## ProcessCallback Response Parsing

`VnPayGateway.ProcessCallback(queryParams)`:

1. Validate signature (returns `Error.Unauthorized` on failure)
2. Extract fields from query params:

   | Field | Query Param | Fallback |
   |-------|-------------|----------|
   | `txnRef` | `vnp_TxnRef` | |
   | `vnpTransactionNo` | `vnp_TransactionNo` | |
   | `amount` | `vnp_Amount` | Divided by 100 |
   | `responseCode` | `vnp_ResponseCode` | |
   | `transactionStatus` | `vnp_TransactionStatus` | |
   | `bankCode` | `vnp_BankCode` | |
   | `cardType` | `vnp_CardType` | |
   | `payDate` | `vnp_PayDate` | |
   | `vnpToken` | `vnp_Token` | Fallback: `vnp_token` (lowercase) |
   | `cardNumber` | `vnp_CardNumber` | Fallback: `vnp_card_number` |

3. Success check: `ResponseCode == "00" && TransactionStatus == "00"`
4. Serialize full query params to JSON as `RawResponseJson`

---

## NormalizeOrderInfo

`VnPayHelper.NormalizeOrderInfo(value)`:

VNPay requires order descriptions to be ASCII-safe. This method:

1. Replaces Vietnamese `D/d` with ASCII equivalent
2. Decomposes Unicode (FormD) and strips non-spacing marks (diacritics)
3. Keeps only allowed characters: `[a-zA-Z0-9]`, space, `.`, `,`, `:`, `-`
4. Replaces disallowed characters with spaces
5. Collapses consecutive whitespace
6. Truncates to **255 characters** maximum

---

## ParseQueryString

`VnPayHelper.ParseQueryString(queryString)`:

- Strips leading `?`
- Splits on `&`
- Decodes keys and values using `Uri.UnescapeDataString` (with `+` replaced by space)
- Returns case-insensitive dictionary

---

## Amount Handling

- **Outgoing** (to VNPay): multiply by 100. A 100,000 VND transaction sends `vnp_Amount = 10000000`
- **Incoming** (from VNPay): divide by 100. `vnp_Amount = 10000000` becomes 100,000 VND
- Internal amounts use `decimal` (via `Money` value object); VNPay uses `long` integers

---

## Timezone Handling

All VNPay timestamps use **GMT+7** (Vietnam timezone):

```csharp
var createDate = _clock.UtcNow.AddHours(7);
```

This applies to:
- `vnp_CreateDate`
- `vnp_ExpireDate` (CreateDate + 15 minutes)
- `vnp_TransactionDate` (for query/refund)
- `vnp_RequestId` prefix (for query/refund)

---

## Error Handling

| Method | On HTTP/network failure |
|--------|------------------------|
| `CreatePaymentUrl` | Returns `Error.Unavailable("VnPay.NotConfigured")` if TmnCode/HashSecret missing |
| `ProcessCallback` | Returns `Error.Unauthorized("VnPay.InvalidSignature")` on bad signature |
| `QueryTransactionAsync` | Catches all exceptions, returns `Error.Unavailable("VnPay.QueryFailed")` |
| `RefundAsync` | Catches all exceptions, returns `Error.Unavailable("VnPay.RefundFailed")` |
| `RemoveTokenAsync` | Catches all exceptions, returns `Error.Unavailable("VnPay.TokenRemoveFailed")` |

All methods check `EnsureConfigured()` before proceeding (verifies `TmnCode` and `HashSecret` are non-empty).
