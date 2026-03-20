# 01 - Create Report

## Sequence

```sequenceDiagram
    actor User
    participant API as POST /api/reports
    participant Handler as CreateReportCommandHandler
    participant DB as Database
    participant Notif as NotificationDispatch

    User->>API: { EntityType, EntityId, ReasonCode, Description?, Attachments? }
    API->>Handler: CreateReportCommand
    Handler->>Handler: Validate (EntityType not blank, EntityId not empty, ReasonCode not blank)
    Handler->>DB: Report.Create(reporterId, entityType, entityId, reasonCode, description, attachments, nowUtc)
    Note over DB: Status = Open
    Handler->>DB: dbContext.Insert(report) + SaveChanges
    Handler->>Notif: CreateNotificationCommand(userId=reporter, type=moderation, event=report_created)
    Note over Notif: Title: "Bao cao da duoc ghi nhan"
    Handler-->>API: ReportDto
    API-->>User: 200 OK + ReportDto
```

## POST /api/reports

**Auth:** Authenticated (any user)

### Request Body

```json
{
  "entityType": "Auction",
  "entityId": "3fa85f64-...",
  "reasonCode": "fraud",
  "description": "Optional description",
  "attachments": "optional JSON string"
}
```

### Handler Logic

1. Validate: `EntityType` not whitespace, `EntityId` not empty GUID, `ReasonCode` not whitespace
2. `Report.Create(...)` -- sets `Status = Open`, `CreatedAt = nowUtc`
3. Insert into DB and save
4. Dispatch notification to the reporter confirming receipt:
   - `NotificationType`: `moderation`
   - `EventType`: `report_created`
   - `Title`: "Bao cao da duoc ghi nhan"
   - `Message`: "He thong da ghi nhan bao cao cua ban va se xu ly som."
5. Return `ReportDto`

### Response -- ReportDto

| Field | Type | Description |
|-------|------|-------------|
| Id | Guid | Report ID |
| ReporterId | Guid | User who submitted the report |
| EntityType | string | Type of reported entity (e.g. "Auction") |
| EntityId | Guid | ID of reported entity |
| ReasonCode | string | Reason code for the report |
| Description | string? | Optional description |
| Attachments | string? | Optional attachments (JSON) |
| Status | string | `open`, `under_review`, `action_taken`, `dismissed`, `closed` |
| AssignedTo | Guid? | Admin assigned to review |
| CreatedAt | DateTime | Creation timestamp |
| AssignedAt | DateTime? | When assigned |
| ResolvedAt | DateTime? | When resolved |
| EscalatedEmergencyAt | DateTime? | When escalated to emergency |
| ResolutionNotes | string? | Notes from resolution |

## GET /api/me/reports

**Auth:** Authenticated (any user)

Returns the current user's own reports ordered by `CreatedAt` descending.

**Response:** `IReadOnlyList<ReportDto>`

### Handler Logic (GetMyReportsQuery)

1. Filter `Report` where `ReporterId == currentUser.UserId`
2. Order by `CreatedAt` descending
3. Map to `ReportDto` list

## ReportStatus Enum

| Value | String ID |
|-------|-----------|
| Open | `open` |
| UnderReview | `under_review` |
| ActionTaken | `action_taken` |
| Dismissed | `dismissed` |
| Closed | `closed` |

## Validation & Error Codes

| Field | Validation | Error |
|-------|-----------|-------|
| EntityType | Not whitespace | Validation error |
| EntityId | Not empty GUID | Validation error |
| ReasonCode | Not whitespace | Validation error |
