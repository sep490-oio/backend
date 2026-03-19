# Quan ly Media cua San pham (Manage Item Media)

## Tong quan
Seller quan ly hinh anh va video cua san pham: them, xoa, sap xep thu tu, dat anh chinh. Chi thao tac duoc khi item o trang thai `Draft`.

## Actors
- **Seller** (can quyen `Items.ManageMedia`)

## Endpoint Sequence

### Step 1: Them media vao item

- **Method:** `POST /api/items/{itemId}/media`
- **Auth:** Required - Permission `Items.ManageMedia`
- **Request:**
  ```json
  {
    "mediaUploadId": "guid (required - ID tu buoc Media Upload)",
    "isPrimary": "boolean (default: false)",
    "sortOrder": "int? (optional - thu tu, auto-assign neu null)"
  }
  ```
- **Response:** `201 Created`
- **Loi co the xay ra:**
  - `404 Not Found` - Item hoac MediaUpload khong ton tai
  - `400 Bad Request` - Item khong o trang thai `Draft`
  - `400 Bad Request` - Da dat gioi han so luong media
  - `403 Forbidden` - Item hoac MediaUpload khong thuoc user hien tai

### Step 2: Xoa media khoi item

- **Method:** `DELETE /api/items/{itemId}/media/{mediaId}`
- **Auth:** Required - Permission `Items.ManageMedia`
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item hoac media khong ton tai
  - `400 Bad Request` - Item khong o trang thai cho phep xoa

### Step 3: Sap xep thu tu media

- **Method:** `PUT /api/items/{itemId}/media/reorder`
- **Auth:** Required - Permission `Items.ManageMedia`
- **Request:**
  ```json
  {
    "orderedMediaIds": ["guid", "guid", "guid"]
  }
  ```
- **Response:** `204 No Content`
- **Ghi chu:** Danh sach `orderedMediaIds` phai chua TAT CA media ID cua item, theo thu tu mong muon
- **Loi co the xay ra:**
  - `404 Not Found` - Item khong ton tai
  - `400 Bad Request` - Danh sach ID khong day du hoac co ID khong thuoc item
  - `422 Unprocessable Entity` - Validation loi

### Step 4: Dat anh chinh (primary image)

- **Method:** `POST /api/items/{itemId}/media/{mediaId}/primary`
- **Auth:** Required - Permission `Items.ManageMedia`
- **Response:** `204 No Content`
- **Loi co the xay ra:**
  - `404 Not Found` - Item hoac media khong ton tai
  - `400 Bad Request` - Media khong phai la anh (khong the dat video lam primary)

## Business Logic

### Add Media Handler

1. **Tim item** theo `itemId`, kiem tra ownership (sellerId == currentUser)
2. **Kiem tra trang thai:** Item phai o `Draft`
3. **Tim MediaUpload** theo `mediaUploadId`:
   - Kiem tra ton tai, ownership, context (`item-image` / `item-video`), confirmed, chua linked
4. **Kiem tra gioi han:** `maxForType` tu `UploadContextRegistry` - khong vuot qua so luong toi da
5. **Link media:** `item.AddMedia(nowUtc, upload, isPrimary, maxForType, sortOrder)`
6. **Relocation:** `MediaRelocationService.RelocateLinkedUploadAsync(upload)`
7. **Luu:** `SaveChangesAsync`

### Remove Media Handler

1. **Tim item** theo `itemId`, include `Media`
2. **Tim media** theo `mediaId` trong item
3. **Xoa media:** `item.RemoveMedia(mediaId, nowUtc)`
4. **Raise event:** `MediaRemovedFromItemEvent` (publicId, resourceType)
5. **Luu:** `SaveChangesAsync`

### Reorder Media Handler

1. **Tim item** theo `itemId`, include `Media`
2. **Validate:** Danh sach `orderedMediaIds` phai chua dung so luong va dung cac ID cua media trong item
3. **Cap nhat sortOrder:** Theo thu tu trong danh sach
4. **Luu:** `SaveChangesAsync`

### Set Primary Image Handler

1. **Tim item** theo `itemId`, include `Media`
2. **Tim media** theo `mediaId`
3. **Dat primary:** `item.SetPrimaryImage(mediaId)` - Tat ca media khac `isPrimary = false`, media nay `isPrimary = true`
4. **Luu:** `SaveChangesAsync`

## Domain Events & Side Effects

| Event | Mo ta |
|-------|-------|
| `MediaRemovedFromItemEvent` | Khi media bi xoa. Handler co the xoa file khoi Cloudinary |

## Luu y nghiep vu

- **Chi thao tac khi Item o `Draft`** - sau khi submit, khong the them/xoa/sap xep media
- **Gioi han so luong** media duoc cau hinh theo type (item-image, item-video) qua `UploadContextRegistry`
- **Chi 1 primary image** tai moi thoi diem
- **Khi xoa media**, file tren Cloudinary co the duoc xoa (hoac giu lai tuy cau hinh)
- **Reorder** yeu cau gui TAT CA media IDs - neu thieu se bi loi
- **Media upload** phai duoc confirm truoc khi add vao item
- **SortOrder** quyet dinh thu tu hien thi anh/video trong giao dien
