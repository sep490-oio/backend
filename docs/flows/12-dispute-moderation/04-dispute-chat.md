# 04 - Dispute Chat (Real-time Messaging)

## Overview Sequence

```sequenceDiagram
    actor Client
    participant Hub as SignalR /hubs/disputes
    participant API as REST API
    participant Handler as SendDisputeMessageCommandHandler
    participant DB as Database
    participant Event as DisputeMessageSentEvent
    participant EH as DisputeMessageSentEventHandler
    participant RT as IDisputeRealtimeService
    participant Notif as NotificationDispatch

    Client->>Hub: Connect (authenticated)
    Note over Hub: Auto-join group dispute-user:{userId}
    Client->>Hub: JoinDispute(disputeId)
    Hub->>Hub: Validate access (DisputeAccessService)
    Hub->>Hub: EnsureParticipantState + SaveChanges
    Hub->>Hub: Add to group dispute:{disputeId}
    alt Is Admin
        Hub->>Hub: Add to group dispute:{disputeId}:admins
    end

    Client->>API: POST /api/disputes/{id}/messages
    API->>Handler: SendDisputeMessageCommand
    Handler->>Handler: Validate access + internal permission
    Handler->>Handler: Validate message or attachment present
    Handler->>Handler: Validate media uploads (confirmed, context=dispute_attachment, owned, not linked)
    Handler->>DB: dispute.AddMessage(senderId, message, nowUtc, isInternal)
    Handler->>DB: Create DisputeMessageAttachment(s) + LinkToEntity + relocate media
    Handler->>DB: MarkRead sender for this message
    Handler->>DB: SaveChanges
    Handler->>Event: Publish DisputeMessageSentEvent

    Event->>EH: Handle
    EH->>RT: BroadcastMessageAsync(disputeId, dto, isInternal)
    Note over RT: Internal -> admins group only<br/>External -> dispute:{id} group
    EH->>EH: Calculate unread for each recipient
    loop Each recipient (not sender)
        EH->>RT: BroadcastUnreadUpdatedAsync(recipientId, unreadDto)
        EH->>Notif: CreateNotificationCommand (dispute_message_received or dispute_internal_message_received)
    end

    RT-->>Client: MessageReceived(DisputeMessageDto)
    RT-->>Client: DisputeUnreadUpdated(DisputeUnreadUpdateDto)
```

## SignalR Hub -- `/hubs/disputes`

**Authentication:** Required (`[Authorize]`)

### Client-to-Server Methods

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinDispute` | `Guid disputeId` | Validate access via DisputeAccessService, ensure participant state exists, join `dispute:{disputeId}` group. Admins also join `dispute:{disputeId}:admins`. Throws `HubException` on access failure. |
| `LeaveDispute` | `Guid disputeId` | Remove from `dispute:{disputeId}` group. Admins also removed from `dispute:{disputeId}:admins`. |

### Connection Lifecycle

- **OnConnectedAsync:** Auto-join `dispute-user:{userId}` group (for per-user unread broadcasts)
- **OnDisconnectedAsync:** Auto-leave `dispute-user:{userId}` group

### Server-to-Client Methods (IDisputeHubClient)

| Method | DTO | Description |
|--------|-----|-------------|
| `MessageReceived` | `DisputeMessageDto` | New message in a dispute thread |
| `ReadStateUpdated` | `DisputeParticipantReadStateDto` | A participant's read state changed |
| `DisputeUpdated` | `DisputeThreadMetaDto` | Dispute metadata changed (status, priority, assignment, etc.) |
| `DisputeUnreadUpdated` | `DisputeUnreadUpdateDto` | Unread count changed for a specific user+dispute |

### Groups

| Group Pattern | Members | Used For |
|---------------|---------|----------|
| `dispute:{disputeId}` | All participants who joined | External messages, read state, dispute updates |
| `dispute:{disputeId}:admins` | Admin participants only | Internal messages |
| `dispute-user:{userId}` | Single user across all disputes | Per-user unread count broadcasts |

## REST Endpoints

### POST /api/disputes/{disputeId}/messages

**Auth:** Authenticated (must be participant or admin)
**Idempotency:** Yes (`IdempotencyFilter<DisputeMessageDto>`)

#### Request Body

```json
{
  "message": "Text message content (optional if media attached)",
  "mediaUploadIds": ["guid1", "guid2"],
  "isInternal": false
}
```

#### Handler Logic (SendDisputeMessageCommand)

1. **Validate access:** `DisputeAccessService.GetAccessibleDisputeAsync()` -- 404 if missing, 403 if not participant/admin
2. **Validate internal permission:** `EnsureInternalMessageAllowed(isInternal)` -- 403 `Dispute.InternalMessageForbidden` if non-admin sends internal
3. **Validate content:** Must have either message text or at least one media upload -- error `Dispute.MessageOrAttachmentRequired`
4. **Validate message length:** Max 5000 characters -- error `Dispute.MessageTooLong`
5. **Validate media uploads** (if any):
   - All uploads must exist -- error if missing
   - All must be owned by current user -- error if not
   - All must be confirmed -- error if not
   - All must have context `dispute_attachment` -- error if wrong context
   - All must not be already linked -- error if linked
6. `dispute.AddMessage(senderId, message, nowUtc, isInternal)` -- creates `DisputeMessage`
7. Create `DisputeMessageAttachment` for each upload, insert into DB
8. `upload.LinkToEntity(attachmentId, nowUtc)` + `mediaRelocationService.RelocateLinkedUploadAsync()`
9. `EnsureParticipantState` + `MarkRead` for sender
10. `SaveChanges`
11. Publish `DisputeMessageSentEvent(disputeId, messageId, senderId, isInternal, nowUtc)`

#### Response -- DisputeMessageDto

```json
{
  "id": "guid",
  "disputeId": "guid",
  "senderId": "guid",
  "senderDisplayName": "username",
  "message": "text content",
  "isInternal": false,
  "createdAt": "2026-03-20T...",
  "attachments": [
    {
      "id": "guid",
      "fileName": "photo.jpg",
      "resourceType": "image",
      "secureUrl": "https://...",
      "bytes": 102400,
      "format": "jpg",
      "width": 1920,
      "height": 1080,
      "durationSeconds": null
    }
  ]
}
```

### POST /api/disputes/{disputeId}/read

**Auth:** Authenticated (must be participant or admin)

#### Request Body

```json
{
  "lastReadMessageId": "guid"
}
```

#### Handler Logic (MarkDisputeReadCommand)

1. Validate: `DisputeId` and `LastReadMessageId` not empty GUIDs
2. Validate access via `DisputeAccessService.GetAccessibleDisputeAsync()`
3. Load the message -- must exist in the dispute, and non-admins cannot mark internal messages as read
4. `EnsureParticipantStateAsync()` -- load or create participant state
5. `state.MarkRead(messageId, readAt)` -- updates LastReadMessageId, LastReadAt, LastSeenAt, ModifiedAt
6. Save changes
7. Publish `DisputeReadStateUpdatedEvent(disputeId, userId, lastReadMessageId, readAt)`

#### Response -- DisputeParticipantReadStateDto

```json
{
  "disputeId": "guid",
  "userId": "guid",
  "lastReadMessageId": "guid",
  "lastReadAt": "2026-03-20T..."
}
```

### GET /api/disputes/{disputeId}/messages

**Auth:** Authenticated (must be participant or admin)

#### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| beforeCreatedAt | DateTime? | null | Cursor: only messages before this timestamp |
| beforeId | Guid? | null | Cursor: tie-breaker for same timestamp |
| pageSize | int? | 50 | Items per page (clamped 1-100) |

#### Handler Logic (GetDisputeMessagesQuery)

1. Validate access via `DisputeAccessService.GetAccessibleDisputeAsync()`
2. Query `DisputeMessage` with `Include(Attachments)` where `DisputeId` matches
3. **Non-admins:** filter out `IsInternal = true` messages
4. Apply cursor pagination: `CreatedAt < beforeCreatedAt` (or same time with `Id < beforeId`)
5. Take `pageSize + 1` to determine `hasMore`
6. Resolve sender display names from User table
7. Return messages ordered by `CreatedAt` ascending, then `Id` ascending

#### Response -- DisputeMessagePageDto

```json
{
  "messages": [ /* DisputeMessageDto[] */ ],
  "hasMore": true,
  "nextBeforeCreatedAt": "2026-03-20T...",
  "nextBeforeId": "guid"
}
```

## Event Handlers

### DisputeMessageSentEventHandler

Triggered by `DisputeMessageSentEvent`.

1. Load the message (with attachments) and the dispute
2. Load all participant states for the dispute
3. Gather candidate user IDs: complainant + respondent + assignedTo + all participant states + sender
4. Load users with roles to resolve display names and admin status
5. Build `DisputeMessageDto` with sender display name
6. **Broadcast message:**
   - Internal message -> `BroadcastMessageAsync(disputeId, dto, adminOnly=true)` (sent to `dispute:{id}:admins`)
   - External message -> `BroadcastMessageAsync(disputeId, dto, adminOnly=false)` (sent to `dispute:{id}`)
7. **For each recipient (excluding sender):**
   - Calculate unread count (considers internal visibility based on admin role)
   - `BroadcastUnreadUpdatedAsync(recipientId, unreadDto)` via `dispute-user:{userId}` group
   - Send notification: EventType `dispute_message_received` or `dispute_internal_message_received`
   - Notification message: first 120 chars of message text, or "sent an attachment" / "sent N attachments"

### DisputeReadStateUpdatedEventHandler

Triggered by `DisputeReadStateUpdatedEvent`.

1. Load participant state for the user in the dispute
2. Check if the read message is internal
3. Check if the user is an admin
4. `BroadcastReadStateAsync(disputeId, stateDto, isInternalMessage)` -- broadcast to appropriate group
5. Calculate new unread count
6. `BroadcastUnreadUpdatedAsync(userId, unreadDto)` -- broadcast to user's personal group

### DisputeChangedEventHandler

Triggered by `DisputeChangedEvent` (e.g., after `ResolveDisputeCommand`).

1. Load the dispute
2. `BroadcastDisputeUpdatedAsync(disputeId, metaDto, adminOnly=false)` -- broadcast meta update to all participants
3. **For each participant** (complainant + respondent + assignedTo):
   - Send notification: EventType `dispute_updated`
   - If resolved: "Dispute {number} has been resolved."
   - Otherwise: "Dispute {number} was updated."

## DTO Reference

### DisputeMessageDto

| Field | Type |
|-------|------|
| Id | Guid |
| DisputeId | Guid |
| SenderId | Guid |
| SenderDisplayName | string |
| Message | string |
| IsInternal | bool |
| CreatedAt | DateTime |
| Attachments | IReadOnlyList\<DisputeMessageAttachmentDto\> |

### DisputeMessageAttachmentDto

| Field | Type |
|-------|------|
| Id | Guid |
| FileName | string? |
| ResourceType | string ("image" or "video") |
| SecureUrl | string |
| Bytes | long |
| Format | string |
| Width | int? |
| Height | int? |
| DurationSeconds | double? |

### DisputeParticipantReadStateDto

| Field | Type |
|-------|------|
| DisputeId | Guid |
| UserId | Guid |
| LastReadMessageId | Guid? |
| LastReadAt | DateTime? |

### DisputeThreadMetaDto

| Field | Type |
|-------|------|
| Id | Guid |
| DisputeNumber | string |
| Title | string |
| Status | string |
| Priority | string |
| AuctionId | Guid? |
| VerificationId | Guid? |
| OrderId | Guid? |
| ComplainantId | Guid |
| RespondentId | Guid |
| AssignedTo | Guid? |
| CreatedAt | DateTime |
| ResolvedAt | DateTime? |
| ModifiedAt | DateTime? |

### DisputeUnreadUpdateDto

| Field | Type |
|-------|------|
| DisputeId | Guid |
| UnreadCount | int |
