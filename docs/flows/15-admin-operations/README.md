# 15 - Admin Operations

## Tong quan

Module cac thao tac quan tri he thong danh cho admin, bao gom: quan ly user, phan quyen, duyet san pham, quan ly dau gia, giam sat he thong, xu ly rui ro, quan ly terms, va quan ly tai chinh.

## Cac subflow

| File | Mo ta |
|---|---|
| [user-management.md](./user-management.md) | Quan ly nguoi dung |
| [role-permission.md](./role-permission.md) | Quan ly vai tro va quyen |
| [item-review.md](./item-review.md) | Duyet san pham |
| [auction-curation.md](./auction-curation.md) | Quan ly dau gia |
| [auction-emergency.md](./auction-emergency.md) | Xu ly tinh huong khan cap |
| [sealed-bid-reveal.md](./sealed-bid-reveal.md) | Mo sealed bid |
| [monitoring-alerts.md](./monitoring-alerts.md) | Giam sat canh bao |
| [risk-flags.md](./risk-flags.md) | Quan ly co rui ro |
| [terms-management.md](./terms-management.md) | Quan ly dieu khoan |
| [payment-admin.md](./payment-admin.md) | Quan ly tai chinh |

## Permissions

| Permission | Mo ta |
|---|---|
| `ManageUsers` | Quan ly user, phan quyen, thay doi trang thai |
| `ManageItems` | Duyet san pham, xu ly bao cao, tao emergency |
| `ReadItems` | Xem danh sach san pham, monitoring alerts |
| `ManagePayments` | Duyet rut tien, hoan tien, xem giao dich |
| `ReadPayments` | Xem tong hop tai chinh |

## Admin Base URL

Tat ca admin endpoint bat dau voi `/api/admin/...`

## Endpoints Tong Hop

### User Management
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/users` | Danh sach user |
| `POST` | `/api/admin/users` | Tao user moi |
| `GET` | `/api/admin/users/{userId}` | Chi tiet user |
| `POST` | `/api/admin/users/{userId}/status` | Thay doi trang thai |
| `DELETE` | `/api/admin/users/{userId}` | Xoa user |
| `POST` | `/api/admin/users/{userId}/unlock` | Mo khoa user |
| `PUT` | `/api/admin/users/{userId}/roles/{role}` | Gan vai tro |
| `DELETE` | `/api/admin/users/{userId}/roles/{role}` | Thu hoi vai tro |
| `PUT` | `/api/admin/users/{userId}/permissions/{permission}` | Cap quyen |
| `POST` | `/api/admin/users/{userId}/permissions/{permission}` | Tu choi quyen |
| `DELETE` | `/api/admin/users/{userId}/permissions/{permission}` | Thu hoi quyen |

### Item Moderation
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/items/review-queue` | Hang doi duyet |
| `GET` | `/api/admin/items/{itemId}` | Chi tiet san pham |
| `POST` | `/api/admin/items/{itemId}/approve` | Phe duyet |
| `POST` | `/api/admin/items/{itemId}/reject` | Tu choi |
| `POST` | `/api/admin/items/{itemId}/assign` | Gan reviewer |
| `GET` | `/api/admin/items/{itemId}/reviews` | Lich su review |

### Auction Management
| Method | URL | Mo ta |
|---|---|---|
| `POST` | `/api/admin/auctions/{auctionId}/curation` | Cau hinh curation |
| `POST` | `/api/admin/auctions/{auctionId}/sealed-bids/{sealedBidId}/reveal` | Mo sealed bid |
| `POST` | `/api/admin/auctions/{auctionId}/emergencies` | Tao emergency |
| `POST` | `/api/admin/auctions/{auctionId}/emergencies/{emergencyId}/resolve` | Resolve emergency |
| `POST` | `/api/admin/auctions/{auctionId}/alerts` | Tao alert |
| `POST` | `/api/admin/auctions/{auctionId}/bids/{bidId}/cancel` | Huy bid bat hop le |

### Monitoring & Risk
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/monitoring-alerts` | Danh sach canh bao |
| `POST` | `/api/admin/monitoring-alerts/{alertId}/acknowledge` | Xac nhan nhan alert |
| `POST` | `/api/admin/monitoring-alerts/{alertId}/resolve` | Giai quyet alert |
| `POST` | `/api/admin/users/{userId}/risk-flags` | Tao co rui ro |

### Payment Admin
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/payments/summary` | Tong hop tai chinh |
| `GET` | `/api/admin/payments/transactions` | Danh sach giao dich |
| `GET` | `/api/admin/payments/transactions/{transactionId}` | Chi tiet giao dich |
| `GET` | `/api/admin/payments/escrows` | Danh sach escrow |
| `GET` | `/api/admin/payments/escrows/{escrowId}` | Chi tiet escrow |
| `GET` | `/api/admin/payments/withdrawals` | Danh sach rut tien |
| `POST` | `/api/admin/payments/withdrawals/{withdrawalId}/approve` | Duyet rut tien |
| `POST` | `/api/admin/payments/withdrawals/{withdrawalId}/reject` | Tu choi rut tien |
| `POST` | `/api/payments/vnpay/refund` | Hoan tien VNPay |

### Terms Management
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/terms` | Danh sach terms |
| `POST` | `/api/admin/terms` | Tao terms moi |
| `POST` | `/api/admin/terms/{id}/activate` | Kich hoat terms |
