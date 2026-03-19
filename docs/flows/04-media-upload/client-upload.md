# Client Upload len Cloudinary

## Tong quan
Sau khi nhan duoc signature tu server, client upload file truc tiep len Cloudinary. Buoc nay khong di qua API server cua OIO.

## Actors
- **Client** (browser / mobile app)

## Luong Upload

### Step 1: Chuan bi request

Su dung thong tin tu response cua `POST /api/media/upload-signature`:

```
POST {uploadUrl}
Content-Type: multipart/form-data
```

### Step 2: Tao form data

```
file:         [binary file data]
api_key:      {apiKey}
timestamp:    {timestamp}
signature:    {signature}
public_id:    {publicId}
folder:       {folder}
resource_type: {resourceType}
eager:        {eager}           (neu co)
```

### Step 3: Gui request len Cloudinary

Client gui POST request truc tiep len `uploadUrl` (Cloudinary endpoint).

### Step 4: Nhan response tu Cloudinary

Cloudinary tra ve:
```json
{
  "public_id": "items/pending/abc123/img_a1b2c3d4e5f6",
  "secure_url": "https://res.cloudinary.com/xxx/image/upload/v123/items/pending/abc123/img_a1b2c3d4e5f6.jpg",
  "bytes": 245678,
  "format": "jpg",
  "width": 1200,
  "height": 800,
  "resource_type": "image",
  "original_filename": "my-photo"
}
```

### Step 5: Gui thong tin confirm len OIO server

Client lay thong tin tu response cua Cloudinary va gui len `POST /api/media/confirm` (xem [confirm-upload.md](./confirm-upload.md)).

## Xu ly Loi

| Tinh huong | Xu ly |
|-----------|-------|
| Signature het han | Goi lai `POST /api/media/upload-signature` de lay signature moi |
| File qua kich thuoc | Client validate truoc (`maxFileSize`), Cloudinary se reject |
| Dinh dang khong hop le | Client validate truoc (`allowedFormats`), Cloudinary se reject |
| Upload that bai (network) | Client co the retry upload voi cung signature (neu chua het han) |
| Cloudinary error | Hien thi loi cho user, co the yeu cau signature moi |

## Luu y nghiep vu

- **Upload TRUC TIEP len Cloudinary** - KHONG di qua server OIO -> giam tai server, tang toc do upload
- **Signed upload** dam bao chi nhung upload co chu ky hop le moi duoc chap nhan boi Cloudinary
- **Client can validate truoc:**
  - Kich thuoc file <= `maxFileSize`
  - Dinh dang file nam trong `allowedFormats`
  - Resource type phu hop (image vs video vs raw)
- **Sau khi upload thanh cong**, client BAT BUOC phai goi `POST /api/media/confirm` de xac nhan voi server
- **Neu khong confirm:** MediaUpload se het han signature va file se nam o trang thai `PendingSignature` -> cuoi cung bi cleanup
- **Eager transformations:** Cloudinary co the tu dong tao cac phien ban khac (thumbnail, watermark) neu context co cau hinh `eager`
