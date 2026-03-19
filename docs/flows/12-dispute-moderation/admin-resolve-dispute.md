# Admin Giai Quyet Dispute

## Tong quan

Admin giai quyet dispute giua buyer va seller. Ket qua co the la hoan tien cho buyer, giai ngan cho seller, hoac chia doi. Dispute resolve anh huong truc tiep den escrow.

## Actors

- **Admin** - nguoi co quyen `ResolveDispute`

## Endpoint Sequence

### Giai quyet dispute
- **Method:** `POST /api/admin/disputes/{disputeId}/resolve`
- **Auth:** Required (Permission: `ManageItems` / admin role)
- **Ghi chu:** Command `ResolveDisputeCommand`

## Ket qua Resolve

### Favor Buyer:
- Escrow refund cho buyer
- `EscrowRefundedToBuyerDomainEvent` -> Notification buyer + Audit log

### Favor Seller:
- Escrow release cho seller
- `EscrowReleasedToSellerDomainEvent` -> Notification seller + Audit log

### Compromise:
- Chia phan tram giua buyer va seller
- Tao 2 transaction: refund va release

## Domain Events & Side Effects

- `EscrowReleasedToSellerDomainEvent` -> Audit + Notification
- `EscrowRefundedToBuyerDomainEvent` -> Audit + Notification
- Dispute chuyen sang trang thai `Resolved`
- Order chuyen sang trang thai tuong ung

## Dispute Status

```
Open -> InReview -> Resolved | Closed | Cancelled
```

## Luu y nghiep vu

- Resolve dispute la quyet dinh cuoi cung cua admin
- Escrow bi dong bang suot thoi gian dispute mo
- Admin can doc ky lich su tin nhan truoc khi resolve
- Resolution notes duoc luu de audit va tham khao
