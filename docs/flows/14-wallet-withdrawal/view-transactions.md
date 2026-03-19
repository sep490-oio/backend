# Xem Lich Su Giao Dich (View Transactions)

## Tong quan

User co the xem lich su giao dich wallet bao gom: nap tien, rut tien, dat coc, thanh toan, hoan tien, va cac thao tac hold/unhold.

## Actors

- **User** - xem lich su giao dich cua minh

## Endpoint Sequence

### Xem danh sach giao dich
- **Method:** `GET /api/me/wallet/transactions`
- **Auth:** Required
- **Query params:** `WalletTransactionFilterParameters` (phan trang, loc, sap xep)
- **Response:** `200 OK` -> `PagedList<WalletTransactionDto>`

### Xem chi tiet giao dich
- **Method:** `GET /api/me/wallet/transactions/{transactionId}`
- **Auth:** Required
- **Response:** `200 OK` -> `WalletTransactionDto`

## Transaction Types

| Type | Mo ta |
|---|---|
| `Credit` | Nap tien (VNPay top-up, escrow release, ...) |
| `Debit` | Tru tien (rut tien, thanh toan, ...) |
| `Hold` | Tam giu (dat coc dau gia, rut tien pending) |
| `Unhold` | Giai phong (hoan deposit, tu choi rut tien) |
| `DebitPending` | Tru tu pending (convert deposit thanh payment) |

## Filter Options

- Theo type (Credit, Debit, ...)
- Theo khoang thoi gian (from, to)
- Phan trang va sap xep

## Luu y nghiep vu

- Lich su giao dich la read-only, khong the sua/xoa
- Moi thao tac wallet tu dong tao WalletTransaction
- Transaction chua description mo ta ly do
- Frontend co the filter theo type de xem tung loai giao dich
