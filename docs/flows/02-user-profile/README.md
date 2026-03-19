# 02 - User Profile

## Tong quan

Module quan ly thong tin ca nhan cua nguoi dung: ho so, so dien thoai, dia chi, va ho so nguoi ban.
Tat ca endpoint thuoc nhom `api/me/*` va yeu cau dang nhap.

## Cac subflow

| # | Subflow | File |
|---|---------|------|
| 1 | Cap nhat ho so | [update-profile.md](./update-profile.md) |
| 2 | Xac thuc so dien thoai | [phone-verification.md](./phone-verification.md) |
| 3 | Quan ly dia chi | [address-management.md](./address-management.md) |
| 4 | Ho so nguoi ban | [seller-profile.md](./seller-profile.md) |
| 5 | Tuy chon thong bao | [notification-preferences.md](./notification-preferences.md) |

## Endpoint Map

| Method | URL | Mo ta | Permission |
|--------|-----|-------|------------|
| `GET` | `/api/me` | Lay thong tin user hien tai | - |
| `GET` | `/api/me/profile` | Lay profile chi tiet | - |
| `PUT` | `/api/me/profile` | Cap nhat profile | `Me.UpdateProfile` |
| `PUT` | `/api/me/phone` | Dat so dien thoai | `Me.ManagePhone` |
| `POST` | `/api/me/phone/confirm` | Xac thuc so dien thoai | `Me.ManagePhone` |
| `GET` | `/api/me/addresses` | Lay danh sach dia chi | `Me.ReadAddress` |
| `POST` | `/api/me/addresses` | Them dia chi moi | `Me.ManageAddress` |
| `PUT` | `/api/me/addresses/{addressId}` | Cap nhat dia chi | `Me.ManageAddress` |
| `DELETE` | `/api/me/addresses/{addressId}` | Xoa dia chi | `Me.ManageAddress` |
| `PATCH` | `/api/me/addresses/{addressId}/default` | Dat dia chi mac dinh | `Me.ManageAddress` |
| `POST` | `/api/me/seller-profile` | Tao ho so ban | `Me.ManageSellerProfile` |
| `GET` | `/api/me/seller-profile` | Xem ho so ban | `Me.ReadSellerProfile` |
| `PUT` | `/api/me/seller-profile` | Cap nhat ho so ban | `Me.ManageSellerProfile` |
| `GET` | `/api/me/notification-preferences` | Lay tuy chon thong bao | - |
| `PUT` | `/api/me/notification-preferences` | Cap nhat tuy chon thong bao | - |

## Quan he voi cac module khac

- **Seller Profile** can duoc **Admin verify** truoc khi user co the tao san pham dau gia (xem [03-seller-verification](../03-seller-verification/README.md))
- **Avatar** su dung luong **Media Upload** (xem [04-media-upload](../04-media-upload/README.md))
- **Dia chi** duoc su dung trong luong **Shipping** khi gui/nhan hang
