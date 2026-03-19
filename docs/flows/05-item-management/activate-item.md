# Kich hoat San pham (Activate Item)

## Tong quan
Sau khi item duoc admin chap thuan (Approved), seller kich hoat item de co the tao phien dau gia (Auction).

## Actors
- **Seller** (can quyen `Items.Activate`)

## Endpoint Sequence

### Step 1: Kich hoat item

- **Method:** `POST /api/items/{itemId}/activate`
- **Auth:** Required - Permission `Items.Activate`
- **Request:** Khong co body (chi can `itemId` trong URL)
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Item khong o trang thai `Approved` (hoac `ConditionConfirmed` cho truong hop verify by platform)
  - `403 Forbidden` - Item khong thuoc user hien tai
  - `422 Unprocessable Entity` - Dieu kien kich hoat chua du

## Business Logic (Handler)

1. **Tim item** theo `itemId`, kiem tra ownership
2. **Kiem tra trang thai:** Item phai o `Approved` hoac `ConditionConfirmed`
3. **Validate dieu kien kich hoat:**
   - Item co du media (it nhat 1 anh)
   - Item co primary image
   - Thong tin day du (title, condition)
4. **Kich hoat:** `item.Activate(nowUtc)` -> Trang thai chuyen sang `Active`
5. **Raise event:** `ItemStatusChangedEvent(itemId, "Approved", "Active", nowUtc)`
6. **Luu:** `SaveChangesAsync`

## Sau khi Kich hoat

Item `Active` co the duoc su dung de tao Auction:

- **Method:** `POST /api/items/{itemId}/auctions`
- **Auth:** Required
- **Mo ta:** Tao phien dau gia tu item da kich hoat

Hoac:

- **Method:** `POST /api/auctions`
- **Auth:** Required
- **Mo ta:** Tao phien dau gia (co the tham chieu den item)

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `ItemStatusChangedEvent` | Ghi nhan chuyen trang thai sang `Active` |

## Luu y nghiep vu

- **Activate chi kha dung** khi item da duoc approve (online review) hoac condition confirmed (platform verification)
- **Item `Active`** co the tao nhieu Auction (phien dau gia) khac nhau
- **Khong the huy kich hoat** - mot khi Active, item khong the quay lai trang thai truoc
- Luong tieu bieu:
  ```
  Draft -> Submit -> PendingReview -> Approved -> Activate -> Active -> Tao Auction
  ```
  Hoac (verify by platform):
  ```
  Draft -> Submit(verifyByPlatform) -> PendingInspection -> ... -> ConditionConfirmed -> Activate -> Active
  ```
