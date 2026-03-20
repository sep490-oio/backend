# 02 - Admin Report Management

## Admin Report Workflow

```flowchart
---
config:
  layout: elk
---
flowchart TD
    A[GET /api/admin/reports<br/>Filter: status, entityType, entityId] --> B{Select Report}
    B --> C[POST .../assign<br/>AssignedToUserId]
    C --> D[Report: UnderReview]
    D --> E{Decision}
    E --> F[POST .../resolve<br/>Dismissed=false]
    E --> G[POST .../resolve<br/>Dismissed=true]
    E --> H[POST .../escalate-emergency<br/>Auction reports only]
    F --> I[Report: ActionTaken<br/>Notify reporter: report_resolved]
    G --> J[Report: Dismissed<br/>Notify reporter: report_dismissed]
    H --> K[TriggerAuctionEmergencyCommand]
    K --> L[Report: ActionTaken<br/>EscalatedEmergencyAt set]
    I --> M[Audit log created]
    J --> M
    L --> M
```

## GET /api/admin/reports

**Auth:** `Admin.ReadItems`

### Query Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| status | string? | Filter by status (`open`, `under_review`, `action_taken`, `dismissed`, `closed`) |
| entityType | string? | Filter by entity type (case-insensitive) |
| entityId | Guid? | Filter by specific entity ID |

**Response:** `IReadOnlyList<ReportDto>` ordered by `CreatedAt` descending.

### Handler Logic (GetReportsQuery)

1. Build query on `Report` set
2. Apply optional filters: `Status.Id == status`, `EntityType == entityType`, `EntityId == entityId`
3. Order by `CreatedAt` descending
4. Return mapped `ReportDto` list

## POST /api/admin/reports/{reportId}/assign

**Auth:** `Admin.ManageItems`

### Request Body

```json
{
  "assignedToUserId": "3fa85f64-..."
}
```

### Handler Logic (AssignReportCommand)

1. Validate: `ReportId` and `AssignedToUserId` not empty GUIDs
2. Load report by ID -- 404 if not found
3. Load assignee user by ID -- 404 if not found
4. Capture old state (`assignedTo`, `status`)
5. `report.Assign(assignee.Id, nowUtc)`:
   - Sets `AssignedTo = adminId`
   - Sets `AssignedAt = nowUtc`
   - Sets `Status = UnderReview`
   - Sets `ModifiedAt = nowUtc`
6. **Audit log:** `ModerationAuditService.Log(action: "report_assigned", entityType: "Report", ...)`
   - Old data: previous assignedTo + status
   - New data: new assignedTo + status
7. Save and return `ReportDto`

## POST /api/admin/reports/{reportId}/resolve

**Auth:** `Admin.ManageItems`

### Request Body

```json
{
  "dismissed": false,
  "resolutionNotes": "Action has been taken against the reported item."
}
```

### Handler Logic (ResolveReportCommand)

1. Validate: `ReportId` not empty GUID
2. Load report by ID -- 404 if not found
3. Capture old state (`status`, `resolutionNotes`)
4. `report.Resolve(resolutionNotes, dismissed, nowUtc)`:
   - Sets `ResolutionNotes`
   - Sets `ResolvedAt = nowUtc`
   - Sets `Status` = `Dismissed` if `dismissed=true`, else `ActionTaken`
   - Sets `ModifiedAt = nowUtc`
5. **Audit log:** action = `"report_dismissed"` or `"report_resolved"`
6. Save changes
7. **Notification to reporter:**
   - If dismissed: EventType `report_dismissed`, Title "Bao cao da duoc dong"
   - If not dismissed: EventType `report_resolved`, Title "Bao cao da duoc xu ly"
   - Metadata includes: `reportId`, `resolutionNotes`, `dismissed`
8. Return `ReportDto`

## POST /api/admin/reports/{reportId}/escalate-emergency

**Auth:** `Admin.ManageItems`

### Request Body

```json
{
  "reasonOverride": "Fraudulent listing confirmed by investigation"
}
```

### Handler Logic (EscalateReportEmergencyCommand)

1. Validate: `ReportId` not empty GUID
2. Load report by ID -- 404 if not found
3. **Guard:** `EntityType` must be `"Auction"` (case-insensitive) -- returns validation error `Report.UnsupportedEmergencyEntity` otherwise
4. Build emergency reason: use `ReasonOverride` if provided, otherwise `"Escalated from report {id}: {reasonCode}"`
5. Dispatch `TriggerAuctionEmergencyCommand`:
   - `AuctionId = report.EntityId`
   - `TriggerSource = "report_escalation"`
   - `Reason = emergencyReason`
   - `Payload = report.Attachments ?? "{}"`
6. If trigger fails, return the error
7. `report.MarkEscalated(nowUtc)`:
   - Sets `EscalatedEmergencyAt = nowUtc`
   - Sets `Status = ActionTaken`
   - Sets `ModifiedAt = nowUtc`
8. **Audit log:** action = `"report_escalated_emergency"`, new data includes status, escalatedEmergencyAt, entityId, reason
9. Save and return `ReportDto`

## ModerationAuditService

Every admin action in this flow calls `ModerationAuditService.Log(...)`, which:

1. Extracts the current user's role from claims (`ClaimTypes.Role` or `"role"`)
2. Creates an `AuditLog` entity with:
   - `ActorUserId` = current user
   - `ActorRole` = extracted role
   - `Action` = action string (e.g. `"report_assigned"`)
   - `EntityType`, `EntityId`
   - `OldData` / `NewData` = JSON-serialized snapshots
   - `IpAddress` = null (not captured at service level)
3. Inserts into the database (saved with the same unit of work)
