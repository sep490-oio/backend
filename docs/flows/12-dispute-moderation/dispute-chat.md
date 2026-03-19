# Giao Tiep Trong Dispute (Dispute Chat)

## Tong quan

Dispute cho phep buyer, seller, va admin giao tiep de giai quyet tranh chap. Tin nhan duoc gui real-time qua SignalR va luu tru trong DB. Admin co the gui tin nhan internal (chi admin thay).

## Actors

- **Buyer** - mot ben trong dispute
- **Seller** - mot ben trong dispute
- **Admin** - trung gian giai quyet

## Endpoint Sequence

### Xem danh sach dispute cua toi
- **Method:** `GET /api/disputes`
- **Auth:** Required
- **Response:** `200 OK` -> Danh sach dispute ma user co quyen truy cap

### Xem chi tiet dispute
- **Method:** `GET /api/disputes/{disputeId}`
- **Auth:** Required
- **Response:** `200 OK` -> `DisputeThreadDto`
- **Ghi chu:** Chi buyer/seller lien quan hoac admin moi co quyen xem

### Xem tin nhan
- **Method:** `GET /api/disputes/{disputeId}/messages`
- **Auth:** Required
- **Response:** `200 OK` -> `DisputeMessageDto[]`

### Gui tin nhan
- **Method:** `POST /api/disputes/{disputeId}/messages`
- **Auth:** Required
- **Request:**
  ```json
  {
    "message": "Noi dung tin nhan",
    "mediaUploadIds": ["guid1"],
    "isInternal": false
  }
  ```
- **Response:** `201 Created` -> `DisputeMessageDto`
- **Ghi chu:**
  - `isInternal = true`: chi admin thay (tin nhan noi bo)
  - Co the dinh kem media (anh, video)
  - Idempotency filter duoc ap dung de tranh gui trung

### Danh dau da doc
- **Method:** `PATCH /api/disputes/{disputeId}/read`
- **Auth:** Required
- **Response:** `200 OK`

## Real-time (SignalR)

- Tin nhan duoc push real-time qua SignalR hub
- Handler: `DisputeRealtimeEventHandlers`
- Tat ca participant nhan tin nhan ngay lap tuc

## Luu y nghiep vu

- Dispute co the duoc tao tu nhieu nguon: return rejection, verification correction, ...
- Admin co the gui tin nhan internal de trao doi voi admin khac
- Media dinh kem duoc luu qua media upload flow
- Idempotency filter ngan gui tin nhan trung khi user bam nhieu lan
