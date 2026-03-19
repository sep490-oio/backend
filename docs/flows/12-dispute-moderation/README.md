# 12 - Dispute & Moderation

## Tong quan

Module quan ly bao cao vi pham (reports), tranh chap (disputes), va cac cong cu kiem duyet cho admin. Bao gom: user bao cao vi pham, admin xu ly bao cao, giao tiep trong dispute, va xu ly khau cap (emergency escalation).

## Thanh phan chinh

| Thanh phan | Mo ta |
|---|---|
| **Report** | Bao cao vi pham tu user |
| **Dispute** | Tranh chap giua buyer/seller (co the lien ket voi order/auction) |
| **DisputeMessage** | Tin nhan trong dispute thread |
| **MonitoringAlert** | Canh bao tu he thong (collusion, non-payment, ...) |

## Cac subflow

| File | Mo ta |
|---|---|
| [create-report.md](./create-report.md) | User bao cao vi pham |
| [admin-report-management.md](./admin-report-management.md) | Admin xu ly bao cao |
| [dispute-chat.md](./dispute-chat.md) | Giao tiep trong dispute |
| [admin-resolve-dispute.md](./admin-resolve-dispute.md) | Admin giai quyet dispute |
| [auction-emergency.md](./auction-emergency.md) | Xu ly tinh huong khau cap auction |

## Endpoints

### Reports (User)
| Method | URL | Mo ta |
|---|---|---|
| `POST` | `/api/reports` | Tao bao cao vi pham |
| `GET` | `/api/me/reports` | Danh sach bao cao cua toi |

### Disputes (User)
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/disputes` | Danh sach dispute cua toi |
| `GET` | `/api/disputes/{disputeId}` | Chi tiet dispute |
| `GET` | `/api/disputes/{disputeId}/messages` | Tin nhan trong dispute |
| `POST` | `/api/disputes/{disputeId}/messages` | Gui tin nhan |
| `PATCH` | `/api/disputes/{disputeId}/read` | Danh dau da doc |

### Admin
| Method | URL | Mo ta |
|---|---|---|
| `GET` | `/api/admin/reports` | Danh sach tat ca bao cao |
| `POST` | `/api/admin/reports/{reportId}/assign` | Gan nguoi xu ly |
| `POST` | `/api/admin/reports/{reportId}/resolve` | Giai quyet bao cao |
| `POST` | `/api/admin/reports/{reportId}/escalate-emergency` | Nang cap khan cap |
| `POST` | `/api/admin/disputes/{disputeId}/resolve` | Giai quyet dispute |
| `POST` | `/api/admin/auctions/{auctionId}/emergencies` | Tao auction emergency |
| `POST` | `/api/admin/auctions/{auctionId}/emergencies/{emergencyId}/resolve` | Giai quyet emergency |

## Domain Events

- `DisputeMessageSent` -> Real-time notification qua SignalR
- `AuctionApprovedEvent` / `AuctionRejectedEvent` -> Notification seller
