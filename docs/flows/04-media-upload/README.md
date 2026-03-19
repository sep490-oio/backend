# 04 - Media Upload

## Tong quan

Module xu ly upload media (hinh anh, video) su dung **Cloudinary** voi co che **signed upload**. Luong upload gom 3 buoc: yeu cau chu ky, client upload truc tiep len Cloudinary, xac nhan upload thanh cong. File duoc upload vao folder tam (`pending/`) va duoc di chuyen sang folder chinh thuc khi duoc lien ket voi entity (item, verification, avatar, v.v.).

## Kien truc

```
Client                          OIO API                    Cloudinary
  |                                |                          |
  |-- 1. Request Signature ------->|                          |
  |                                |-- Generate signature --->|
  |<-- Return signature + params --|                          |
  |                                                           |
  |-- 2. Direct Upload (signed) ---------------------------->|
  |<-- Upload response (publicId, secureUrl) -----------------|
  |                                                           |
  |-- 3. Confirm Upload ---------->|                          |
  |                                |-- Mark confirmed ------->|
  |<-- Confirmed response ---------|                          |
  |                                                           |
  |                    [Background Job]                       |
  |                         |                                 |
  |                         |-- Relocate file: pending/ ---> final/
  |                         |-- Update URLs & snapshots
```

## Cac subflow

| # | Subflow | File |
|---|---------|------|
| 1 | Yeu cau chu ky upload | [request-signature.md](./request-signature.md) |
| 2 | Client upload len Cloudinary | [client-upload.md](./client-upload.md) |
| 3 | Xac nhan upload | [confirm-upload.md](./confirm-upload.md) |
| 4 | Background relocation & cleanup | [background-relocation-cleanup.md](./background-relocation-cleanup.md) |

## Endpoint Map

| Method | URL | Mo ta | Permission |
|--------|-----|-------|------------|
| `POST` | `/api/media/upload-signature` | Yeu cau chu ky upload | `Media.Upload` |
| `POST` | `/api/media/confirm` | Xac nhan upload thanh cong | `Media.ConfirmUpload` |
| `GET` | `/api/media/contexts` | Lay danh sach upload contexts | `Media.ReadContexts` |

## Upload Contexts

He thong su dung **UploadContextRegistry** de dinh nghia cac context upload khac nhau. Moi context co cau hinh rieng:

| Context | Folder | Resource Type | Max Size | Allowed Formats |
|---------|--------|---------------|----------|-----------------|
| `item-image` | `items` | `image` | Theo cau hinh | jpg, png, webp |
| `item-video` | `items` | `video` | Theo cau hinh | mp4, webm |
| `verification` | `verifications` | `image` | Theo cau hinh | jpg, png |
| `user-avatar` | `avatars` | `image` | Theo cau hinh | jpg, png, webp |
| `category-icon` | `categories` | `image` | Theo cau hinh | png, svg |
| `terms-document` | `terms` | `raw` | Theo cau hinh | pdf |
| `warehouse-inspection` | `inspections` | `image` | Theo cau hinh | jpg, png |
| `dispute-attachment` | `disputes` | `image` | Theo cau hinh | jpg, png |

## Trang thai MediaUpload

```
[PendingSignature]
     |
     | Client upload len Cloudinary
     | POST /api/media/confirm
     v
[Confirmed]
     |
     | Entity link (vd: CreateItem, UploadVerificationDocument)
     v
[Linked]
     |
     | Background PendingUploadRelocationJob
     v
[Relocated]

--- Nhanh khac ---

[Confirmed] -- Khong duoc link trong thoi han --> [Orphan] -- Cleanup --> [Deleted]
[PendingSignature] -- Het han signature --> [Expired] -- Cleanup --> [Deleted]
```

## Luu y nghiep vu

- **Signed upload:** Client KHONG upload truc tiep qua API server. Server chi cap signature, client upload thang len Cloudinary
- **Idempotency:** Ca 2 endpoint (`request-signature`, `confirm`) deu co `IdempotencyFilter` de tranh duplicate
- **Pending folder:** File moi upload vao `{context}/pending/{userId}/`. Khi duoc link voi entity, file duoc di chuyen sang `{context}/{entityId}/`
- **Orphan cleanup:** File upload nhung khong duoc link trong thoi han se bi danh dau `Orphan` va xoa
- **MediaUploadId** la key de cac module khac tham chieu (CreateItem, UploadVerificationDocument, UpdateProfile...)
