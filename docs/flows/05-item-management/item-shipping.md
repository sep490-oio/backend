# Giao hang San pham (Item Shipping)

## Tong quan
Khi seller chon `verifyByPlatform = true`, seller can gui hang den kho OIO de kiem tra chat luong. Endpoint nay cho phep seller cung cap thong tin van chuyen.

## Actors
- **Seller** (can quyen `Items.Create`)

## Endpoint Sequence

### Step 1: Chon phuong thuc van chuyen

- **Method:** `POST /api/items/{itemId}/shipping`
- **Auth:** Required - Permission `Items.Create`
- **Request:**
  ```json
  {
    "senderName": "string (required - ten nguoi gui)",
    "senderPhone": "string (required - so dien thoai nguoi gui)",
    "senderAddress": "string (required - dia chi gui)",
    "senderWard": "string (required - phuong/xa)",
    "senderDistrict": "string (required - quan/huyen)",
    "senderProvince": "string (required - tinh/thanh pho)",
    "weightGrams": "int (required - trong luong, gram)",
    "insuranceValue": "decimal (required - gia tri bao hiem)",
    "providerCode": "string? (optional - ma nha van chuyen, vd: 'GHN', 'GHTK')",
    "senderCarrierAddressDataJson": "string? (optional - du lieu dia chi theo format nha van chuyen)",
    "lengthCm": "int? (optional - chieu dai, cm)",
    "widthCm": "int? (optional - chieu rong, cm)",
    "heightCm": "int? (optional - chieu cao, cm)",
    "externalTrackingNumber": "string? (optional - ma van don ngoai, neu seller tu ship)",
    "externalCarrierName": "string? (optional - ten hang van chuyen ngoai)",
    "notes": "string? (optional - ghi chu)"
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid (shipping record ID)",
    "itemId": "guid",
    "trackingNumber": "string? (neu su dung nha van chuyen tich hop)",
    "status": "string",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai cho phep (phai la `PendingInspection`)
  - `403 Forbidden` - Item khong thuoc user hien tai
  - `422 Unprocessable Entity` - Validation loi

## Business Logic (Handler)

1. **Tim item** theo `itemId`, kiem tra ownership
2. **Kiem tra trang thai:** Item phai o `PendingInspection`
3. **Validate thong tin:** Kiem tra cac truong bat buoc
4. **Tao shipping record:**
   - Neu co `providerCode` (vd: GHN): Tich hop voi API nha van chuyen de tao don
   - Neu co `externalTrackingNumber`: Seller tu ship, chi luu tracking info
5. **Tao inbound shipment** qua Warehouse context:
   - `POST /api/warehouse/inbound-shipments` (noi bo)
   - Lien ket shipment voi item
6. **Luu:** `SaveChangesAsync`

## Lien ket voi Warehouse

Sau khi tao shipping, luong tiep theo thuoc Warehouse context:

```
[Seller gui hang]
     |
     v
[InboundShipment Created] --> GHN webhook cap nhat tracking
     |
     v
[Kho nhan hang] --> Kiem tra (xem item-qa.md)
```

## Luu y nghiep vu

- **Chi ap dung khi `verifyByPlatform = true`** - Item can gui den kho OIO
- **Nha van chuyen tich hop:** He thong tich hop GHN (Giao Hang Nhanh) de tu dong tao don va tracking
- **Tu ship:** Seller co the tu gui bang nha van chuyen khac, chi can cung cap tracking number
- **Insurance value** giup bao hiem hang hoa trong qua trinh van chuyen
- **Kich thuoc** (length, width, height) giup tinh phi van chuyen chinh xac
- **Dia chi nguoi gui** thuong lay tu dia chi cua seller, nhung co the nhap khac
- **Webhook GHN** tu dong cap nhat trang thai shipment khi hang duoc van chuyen
