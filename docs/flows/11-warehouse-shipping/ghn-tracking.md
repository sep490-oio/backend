# Theo Doi Van Chuyen GHN (GHN Tracking Webhook)

## Tong quan

GHN gui webhook moi khi trang thai don van chuyen thay doi. He thong nhan webhook, parse du lieu, cap nhat trang thai outbound shipment, va trigger domain events de cap nhat order.

## Actors

- **GHN** - gui webhook cap nhat trang thai
- **System** - xu ly webhook, cap nhat shipment/order

## Endpoint

### GHN Webhook
- **Method:** `POST /webhooks/ghn`
- **Auth:** Anonymous (GHN goi truc tiep)
- **Request:** JSON body tu GHN chua thong tin trang thai
- **Response:** Luon tra ve `200 OK`
- **Ghi chu:** `ExcludeFromDescription()` - khong hien thi tren Swagger/Scalar

## Business Logic Chi Tiet

### Buoc 1: Doc raw body
- Doc body tu HTTP request
- Neu body rong: log warning va tra ve 200 OK

### Buoc 2: Resolve GHN provider
- `IShippingProviderSelector.Select("ghn")`
- Neu khong tim thay provider: log error, tra ve 200 OK

### Buoc 3: Load GHN config
- Tim `ShippingProviderConfig` co `ProviderCode = ghn` va `IsActive = true`
- Neu khong co config: log error, tra ve 200 OK

### Buoc 4: Parse & Verify webhook
- `provider.ParseWebhook(body, headers, query, config)`
- Verify ShopId trong body phai khop voi config
- Neu parse/verify that bai: log warning, tra ve 200 OK

### Buoc 5: Dispatch command
- Tao `ProcessTrackingWebhookCommand` voi:
  - `ProviderCode` - "ghn"
  - `ClientOrderCode` - Ma don hang noi bo
  - `CarrierTrackingNumber` - Ma van don GHN
  - `CarrierStatusRaw` - Ma trang thai raw tu GHN
  - `NormalizedStatusId` - Trang thai da chuyen doi
  - `Location` - Vi tri hien tai
  - `EventTime` - Thoi gian su kien
  - `RawPayloadJson` - JSON goc

### Buoc 6: Luon tra ve 200 OK
- Tra ve 200 bat ke thanh cong hay that bai
- Non-200 response se khien GHN retry vo han

## GHN Status Mapping

| GHN Status | Normalized Status | Domain Event |
|---|---|---|
| `ready_to_pick` | `ReadyToPick` | - |
| `picking` | `Picking` | - |
| `picked` | `PickedUp` | `OutboundShipmentPickedUpEvent` |
| `delivering` | `InTransit` | - |
| `delivered` | `Delivered` | `OutboundShipmentDeliveredEvent` |
| `return` | `Returning` | - |
| `returned` | `Returned` | - |
| `cancel` | `Cancelled` | - |

## Cap nhat Shipping Provider Config

- **Method:** `PUT /api/warehouse/shipping-provider-configs/{configId}`
- **Auth:** Required
- Cap nhat API key, ShopId, endpoint cua GHN

## Luu y nghiep vu

- Luon tra ve 200 OK cho GHN de tranh retry loop
- Parse error duoc log va bo qua (khong anh huong ket qua)
- GHN verify bang ShopId trong body (khong co HMAC/bearer token)
- Moi webhook event duoc luu de debug va audit
- Tracking webhook la nguon du lieu chinh de cap nhat trang thai order
