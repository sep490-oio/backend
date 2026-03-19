# Tao San pham (Create Item)

## Tong quan
Seller tao san pham moi de dau gia. Co the dinh kem media (anh/video) ngay khi tao. Item duoc tao o trang thai `Draft`.

## Actors
- **Seller da xac minh** (can quyen `Items.Create`)

## Endpoint Sequence

### Tien quyet: Upload media (neu co)

Neu muon dinh kem anh/video khi tao item, seller can upload truoc qua luong **Media Upload**:
1. `POST /api/media/upload-signature` (context: `item-image` hoac `item-video`)
2. Upload len Cloudinary
3. `POST /api/media/confirm`
4. Nhan `mediaUploadId` de truyen vao request tao item

### Step 1: Tao item

- **Method:** `POST /api/items`
- **Auth:** Required - Permission `Items.Create`
- **Request:**
  ```json
  {
    "title": "string (required - tieu de san pham, toi da theo App.Constraint.Item.TitleMaxLength)",
    "condition": "string (required - 'New', 'LikeNew', 'Good', 'Fair', 'Poor')",
    "categoryId": "guid? (optional - danh muc san pham)",
    "description": "string? (optional - mo ta chi tiet)",
    "quantity": "int (default: 1, phai > 0)",
    "attributes": "object? (optional - thuoc tinh tu do, luu dang JSON)",
    "images": [
      {
        "mediaUploadId": "guid (required - ID tu buoc upload truoc)",
        "isPrimary": "boolean (default: false - anh chinh?)",
        "sortOrder": "int (default: 0 - thu tu sap xep)"
      }
    ]
  }
  ```
- **Response:** `201 Created`
  ```json
  {
    "id": "guid",
    "title": "string",
    "condition": "string",
    "status": "Draft",
    "categoryId": "guid?",
    "description": "string?",
    "quantity": 1,
    "media": [
      {
        "id": "guid",
        "secureUrl": "string",
        "isPrimary": "boolean",
        "sortOrder": "int",
        "resourceType": "string"
      }
    ],
    "sellerId": "guid",
    "createdAt": "datetime"
  }
  ```
- **Loi co the xay ra:**
  - `422 Unprocessable Entity` - Validation loi
  - `404 Not Found` - Category khong ton tai
  - `400 Bad Request` - MediaUpload khong ton tai, khong thuoc user, chua confirm, da linked, sai context
  - `401 Unauthorized` - Chua dang nhap
  - `403 Forbidden` - Khong co quyen

### Step (tham khao): Xem san pham cua toi

- **Method:** `GET /api/items/my`
- **Auth:** Required (Authenticated)
- **Query Parameters:** Filter theo trang thai, phan trang
- **Response:** `200 OK` (danh sach phan trang)

### Step (tham khao): Xem chi tiet item

- **Method:** `GET /api/items/{itemId}`
- **Auth:** Anonymous (ai cung xem duoc item public)
- **Response:** `200 OK`

## Business Logic (Handler)

1. **Validate category** (neu co):
   - `CategoryId.From(request.CategoryId)`
   - Kiem tra category ton tai trong DB

2. **Validate media uploads** (neu co):
   - Tim tat ca `MediaUpload` theo `mediaUploadIds`
   - **Kiem tra ton tai:** Moi ID phai co trong DB
   - **Kiem tra ownership:** Phai thuoc `currentUser.UserId`
   - **Kiem tra confirmed:** Phai o trang thai `Confirmed`
   - **Kiem tra chua linked:** Khong duoc da link voi entity khac
   - **Kiem tra context:** Phai la `item-image` hoac `item-video` (su dung `contextRegistry.IsItemContext`)

3. **Tao Value Objects:**
   - `ItemTitle.Create(title)` - Validate va normalize tieu de
   - `ItemCondition.FromId(condition)` - Parse enum condition

4. **Tao Item aggregate:**
   ```
   Item.Create(sellerId, title, condition, nowUtc, categoryId, description, quantity, attributes)
   ```

5. **Link media** (neu co):
   - Duyet qua `request.Media` theo `SortOrder`
   - Tim `MediaUpload` tuong ung
   - `item.AddMedia(nowUtc, upload, isPrimary, maxForType, sortOrder)`
   - Kiem tra gioi han so luong media theo type (`maxForType` tu `UploadContextRegistry`)

6. **Relocation ngay:** Neu co media, goi `MediaRelocationService.RelocateLinkedUploadAsync` cho tung upload

7. **Luu:** Insert item, `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `ItemCreatedEvent` | Phat ra khi item duoc tao thanh cong (itemId, sellerId, title) |

## Validation Rules

| Field | Rule |
|-------|------|
| `title` | Khong rong, do dai toi da theo `App.Constraint.Item.TitleMaxLength` |
| `condition` | Phai thuoc: `New`, `LikeNew`, `Good`, `Fair`, `Poor` |
| `categoryId` | Neu co, phai la Guid hop le va category phai ton tai |
| `description` | Neu co, khong duoc rong (whitespace) |
| `quantity` | > 0, khong duoc la 0 |
| `images[].mediaUploadId` | Guid hop le, khong rong |

## Luu y nghiep vu

- **Item moi luon o trang thai `Draft`** - seller co the tiep tuc chinh sua, them/xoa media truoc khi submit
- **Attributes** la JSON tu do, cho phep luu bat ky thuoc tinh nao (vd: brand, model, year, v.v.)
- **Media co gioi han** so luong theo type (vd: toi da 10 anh, 1 video) - cau hinh qua `UploadContextRegistry`
- **Chi 1 anh duoc dat lam primary** - anh chinh hien thi tren danh sach
- **Media relocation** co the xay ra ngay tai buoc tao (hoac qua background job neu that bai)
- **Seller phai co role `Seller`** (tu Seller Profile verified) de co quyen `Items.Create`
