# 05 - Item Management

## Tong quan

Module quan ly san pham dau gia: tao, gan media, gui duyet, admin review, kich hoat, giao hang va kiem tra chat luong. Item la doi tuong trung tam cua he thong dau gia - moi item co the tao ra 1 hoac nhieu phien dau gia (Auction).

## State Machine

```
[Chua tao]
    |
    | POST /api/items
    v
[Draft]
    |
    +-- Them/Xoa/Sap xep media
    +-- Cap nhat thong tin
    |
    | POST /api/items/{id}/submit
    v
[PendingReview]  <-- verifyByPlatform = true --> [PendingInspection]
    |                                                    |
    +-- Admin Approve                        Warehouse Inspect
    |       |                                            |
    |       v                                            v
    |   [Approved]                              [InspectionCompleted]
    |       |                                            |
    |       | POST /api/items/{id}/activate              | Seller confirm condition
    |       v                                            v
    |   [Active]                                [ConditionConfirmed]
    |       |                                            |
    |       +-- Tao Auction                              +-- Tao Auction
    |
    +-- Admin Reject
            |
            v
        [Rejected]
            |
            +-- POST /api/items/{id}/resubmit --> [PendingReview]
```

## Cac subflow

| # | Subflow | File |
|---|---------|------|
| 1 | Tao san pham | [create-item.md](./create-item.md) |
| 2 | Quan ly media | [manage-media.md](./manage-media.md) |
| 3 | Gui duyet san pham | [submit-for-review.md](./submit-for-review.md) |
| 4 | Admin duyet | [admin-review.md](./admin-review.md) |
| 5 | Kich hoat san pham | [activate-item.md](./activate-item.md) |
| 6 | Giao hang san pham | [item-shipping.md](./item-shipping.md) |
| 7 | Kiem tra chat luong (QA) | [item-qa.md](./item-qa.md) |

## Endpoint Map

### Seller Endpoints

| Method | URL | Mo ta | Permission |
|--------|-----|-------|------------|
| `POST` | `/api/items` | Tao item moi | `Items.Create` |
| `GET` | `/api/items/my` | Xem san pham cua toi | Authenticated |
| `GET` | `/api/items/{itemId}` | Xem chi tiet item | Anonymous |
| `POST` | `/api/items/{itemId}/media` | Them media | `Items.ManageMedia` |
| `DELETE` | `/api/items/{itemId}/media/{mediaId}` | Xoa media | `Items.ManageMedia` |
| `PUT` | `/api/items/{itemId}/media/reorder` | Sap xep media | `Items.ManageMedia` |
| `POST` | `/api/items/{itemId}/media/{mediaId}/primary` | Dat anh chinh | `Items.ManageMedia` |
| `POST` | `/api/items/{itemId}/submit` | Gui duyet | `Items.Create` |
| `POST` | `/api/items/{itemId}/resubmit` | Gui lai | `Items.Resubmit` |
| `POST` | `/api/items/{itemId}/activate` | Kich hoat | `Items.Activate` |
| `POST` | `/api/items/{itemId}/shipping` | Chon van chuyen | `Items.Create` |
| `POST` | `/api/items/{itemId}/confirm-inspected-condition` | Xac nhan tinh trang | `Items.Resubmit` |
| `POST` | `/api/items/{itemId}/auctions` | Tao auction tu item | - |

### Admin Endpoints

| Method | URL | Mo ta | Permission |
|--------|-----|-------|------------|
| `GET` | `/api/admin/items/review-queue` | Hang doi duyet | `Admin.ReadItems` |
| `GET` | `/api/admin/items/{itemId}` | Chi tiet item | `Admin.ReadItems` |
| `GET` | `/api/admin/items/{itemId}/reviews` | Lich su review | `Admin.ReadItems` |
| `POST` | `/api/admin/items/{itemId}/approve` | Chap thuan | `Admin.ManageItems` |
| `POST` | `/api/admin/items/{itemId}/reject` | Tu choi | `Admin.ManageItems` |
| `POST` | `/api/admin/items/{itemId}/assign` | Gan reviewer | `Admin.ManageItems` |

## Domain Events

| Event | Mo ta |
|-------|-------|
| `ItemCreatedEvent` | Khi item duoc tao |
| `ItemStatusChangedEvent` | Khi trang thai item thay doi |
| `ItemSubmittedEvent` | Khi item duoc gui duyet |
| `ItemApprovedEvent` | Khi item duoc admin chap thuan |
| `ItemRejectedEvent` | Khi item bi admin tu choi |
| `MediaRemovedFromItemEvent` | Khi media bi xoa khoi item |
| `ItemQuestionAskedEvent` | Khi co cau hoi ve item |
| `ItemQuestionAnsweredEvent` | Khi seller tra loi cau hoi |

## Luu y nghiep vu

- Seller can co **Seller Profile** da duoc verify va **Identity Verification** da approved de tao item
- Item co the co nhieu media (anh, video), 1 anh chinh (primary), sap xep theo thu tu
- `verifyByPlatform = true`: Item can gui den kho OIO de kiem tra chat luong truoc khi dau gia
- `verifyByPlatform = false`: Chi can admin review online
- Item `Active` co the tao nhieu Auction (phien dau gia)
