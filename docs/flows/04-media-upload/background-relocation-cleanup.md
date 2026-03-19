# Background Relocation & Cleanup

## Tong quan
He thong co cac background job xu ly di chuyen file tu folder tam sang folder chinh thuc (relocation) va don dep file khong su dung (cleanup).

## Background Jobs

### 1. PendingUploadRelocationJob (Quartz - DisallowConcurrentExecution)

**Muc dich:** Di chuyen file da duoc link tu folder tam (`pending/`) sang folder chinh thuc.

**Lich chay:** Dinh ky (cau hinh qua Quartz scheduler)

**Logic:**

1. **Tim candidates:** Query MediaUpload thoa dieu kien:
   - `IsLinked = true` (da duoc link voi entity)
   - `RelocatedAt = null` (chua duoc di chuyen)
   - `StorageRef.Folder` chua `"/pending/"` (van o folder tam)
   - `NextRelocationAttemptAt = null` hoac `<= nowUtc` (da den luc retry)
   - Sap xep theo `NextRelocationAttemptAt` hoac `LinkedAt` hoac `CreatedAt`
   - Lay toi da **50 records** moi batch

2. **Xu ly tung upload:**
   - Goi `IMediaRelocationService.RelocateLinkedUploadAsync(upload)`

3. **Luu ket qua:** `SaveChangesAsync`

### MediaRelocationService - Chi tiet

**Luong relocation cho moi upload:**

1. **Kiem tra dieu kien:**
   - `RequiresRelocation()` = true
   - `EntityId` phai co gia tri
   - Chua den `NextRelocationAttemptAt`

2. **Xac dinh target folder:**
   - Format: `{contextConfig.Folder}/{entityId}`
   - Vd: `items/pending/user123/img_abc` -> `items/item456/img_abc`
   - Truong hop dac biet: Dispute attachments -> `disputes/{disputeId}/messages/{messageId}`

3. **Goi Cloudinary rename:**
   ```
   mediaSignatureService.RenameResourceAsync(
     oldPublicId, targetPublicId, resourceType)
   ```

4. **Cap nhat entity snapshot:** Tuy theo context:
   - **Item context:** `item.RefreshMediaSnapshot(oldPublicId, newStorageRef, newInfo, nowUtc)`
   - **Verification context:** `verification.RefreshDocumentSnapshot(...)`
   - **User avatar context:** `user.RefreshAvatarSnapshot(oldPublicId, newAvatarUrl, nowUtc)`
   - **Terms context:** `document.RefreshMediaSnapshot(newStorageRef, newInfo)`
   - **Category context:** `category.RefreshIconMedia(newStorageRef, newInfo, nowUtc)`
   - **Warehouse inspection:** `inspection.RefreshEvidenceSnapshot(...)`
   - **Dispute context:** `attachment.RefreshMediaSnapshot(newStorageRef, newInfo, nowUtc)`

5. **Danh dau thanh cong:** `upload.MarkRelocationSucceeded(newStorageRef, newInfo, nowUtc)`

### Co che Retry

Khi relocation that bai:

```
Lan 1 that bai -> Retry sau 1 phut
Lan 2 that bai -> Retry sau 5 phut
Lan 3 that bai -> Retry sau 15 phut
Lan 4+ that bai -> Khong retry nua (exhausted)
```

- `upload.MarkRelocationRetry(errorMessage, nextRetryAt)`
- Toi da **3 lan retry** (MaxRetries = 3)
- Sau khi exhausted, upload van giu trang thai `Linked` nhung co `NextRelocationAttemptAt = null`

### 2. Orphan Cleanup (duoc xu ly qua trang thai MediaUpload)

**Muc dich:** Don dep file upload nhung khong duoc link trong thoi han.

**Logic:**
- Khi confirm upload, he thong dat `OrphanExpiresAt = nowUtc + OrphanExpiration`
- Neu MediaUpload khong duoc link truoc `OrphanExpiresAt` -> danh dau Orphan
- File orphan se duoc xoa khoi Cloudinary va ban ghi bi xoa khoi DB

### 3. Signature Expiration

**Muc dich:** Don dep MediaUpload ma signature da het han nhung client chua upload.

**Logic:**
- Khi tao signature, he thong dat `SignatureExpiresAt = nowUtc + SignatureExpiration`
- Neu client khong upload va confirm truoc khi signature het han -> MediaUpload bi expired
- Ban ghi expired se duoc cleanup

## Folder Structure tren Cloudinary

```
cloudinary/
  items/
    pending/
      {userId}/
        img_abc123.jpg      <-- Moi upload, chua link
    {itemId}/
      img_abc123.jpg        <-- Da link va relocate
  verifications/
    pending/
      {userId}/
        img_def456.jpg
    {verificationId}/
      img_def456.jpg
  avatars/
    pending/
      {userId}/
        img_ghi789.jpg
    {userId}/
      img_ghi789.jpg
  ...
```

## Luu y nghiep vu

- **Relocation la bat dong bo** - Sau khi link, file van o folder `pending/` cho den khi job chay
- **Entity snapshot duoc cap nhat** - Khi relocate, URL trong entity (Item, Verification, v.v.) duoc cap nhat tu dong
- **SecureUrl duoc derive** - Neu Cloudinary khong tra ve secureUrl moi, he thong tu replace oldPublicId bang newPublicId trong URL cu
- **Concurrent safety** - Job co `DisallowConcurrentExecution` de tranh race condition
- **Batch processing** - Xu ly toi da 50 records/lan de tranh overload
- **Entity-aware** - He thong biet loai entity (Item, Verification, User, v.v.) de cap nhat dung snapshot
