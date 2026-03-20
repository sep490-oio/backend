# 12 - Dispute & Moderation Flow

## State Machines

### Report Lifecycle

```stateDiagram-v2
---
config:
  layout: elk
---
    [*] --> Open : Create()
    Open --> UnderReview : Assign(adminId)
    UnderReview --> ActionTaken : Resolve(dismissed=false)
    UnderReview --> Dismissed : Resolve(dismissed=true)
    UnderReview --> ActionTaken : MarkEscalated()
    ActionTaken --> Closed : Close()
    Dismissed --> Closed : Close()
    Closed --> [*]
```

### Dispute Lifecycle

```stateDiagram-v2
---
config:
  layout: elk
---
    [*] --> Draft
    [*] --> Open : Create() / CreateForVerification()
    Draft --> Open
    Open --> UnderReview : AssignTo(adminId)
    UnderReview --> AwaitingResponse
    UnderReview --> Resolved : Resolve(resolutionType)
    AwaitingResponse --> Escalated
    AwaitingResponse --> Resolved : Resolve(resolutionType)
    Escalated --> Resolved : Resolve(resolutionType)
    Resolved --> Closed
    Open --> Cancelled
    UnderReview --> Cancelled
    AwaitingResponse --> Cancelled
    Closed --> [*]
    Cancelled --> [*]
```

### Monitoring Alert Lifecycle

```stateDiagram-v2
---
config:
  layout: elk
---
    [*] --> Open : Create()
    Open --> Acknowledged : Acknowledge(adminId, notes)
    Acknowledged --> Resolved : Resolve(ignored=false)
    Acknowledged --> Ignored : Resolve(ignored=true)
    Open --> Resolved : Resolve(ignored=false)
    Open --> Ignored : Resolve(ignored=true)
    Resolved --> [*]
    Ignored --> [*]
```

## Aggregates Overview

| # | Aggregate | Key Fields | Notes |
|---|-----------|-----------|-------|
| 1 | **Report** | ReporterId, EntityType, EntityId, ReasonCode, Description?, Attachments?, Status, AssignedTo?, ResolutionNotes? | User-submitted report against any entity |
| 2 | **Dispute** | DisputeNumber (DSP-xxx), OrderId, AuctionId?, VerificationId?, ComplainantId, RespondentId, Type, DesiredResolution, Status, Priority, ResolutionType, AssignedTo?, EscalatedTo? | Formal dispute with child collections: Messages, Evidence, Refunds, StatusHistory, ParticipantState |
| 3 | **MonitoringAlert** | EntityType, EntityId, AlertType, Severity (AlertSeverity), Payload (jsonb), Status, Notes?, AcknowledgedBy?, ResolvedBy? | System or admin-generated alert |
| 4 | **AuditLog** | ActorUserId?, ActorRole?, Action, EntityType, EntityId?, OldData (jsonb), NewData (jsonb), IpAddress? | Immutable audit trail via ModerationAuditService |
| 5 | **UserRiskFlag** | UserId, FlagType, Reason?, Severity (RiskFlagSeverity), CreatedBy? | Lived in UserContext; created via admin endpoint |
| 6 | **AdminReviewTask** | EntityType, EntityId, AssignedTo?, Status (AdminReviewTaskStatus), Priority (DisputePriority), DueAt?, CompletedAt? | Generic review task (Open, InProgress, Completed, Cancelled, Overdue) |
| 7 | **ReviewQueue** | EntityType, EntityId, PriorityScore, AssignedTo?, Status (ReviewQueueStatus) | Priority-scored queue (Open, Assigned, InProgress, Completed, Cancelled) |

Supporting entities:

- **DisputeResponseTemplate** -- Name, Category?, Subject?, Body, IsActive

## Endpoints

### User Endpoints

| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | `api/reports` | Authenticated | Create a report |
| GET | `api/me/reports` | Authenticated | Get current user's reports |
| GET | `api/disputes` | Authenticated | List accessible disputes (paged; admin sees all, user sees own) |
| GET | `api/disputes/{disputeId}` | Authenticated | Get dispute thread (meta + participants + recent messages) |
| GET | `api/disputes/{disputeId}/messages` | Authenticated | Cursor-paginated messages (internal filtered for non-admins) |
| POST | `api/disputes/{disputeId}/messages` | Authenticated | Send dispute message (idempotent; supports media attachments) |
| POST | `api/disputes/{disputeId}/read` | Authenticated | Mark dispute read up to a message |

### Admin Endpoints

| Method | URL | Permission | Description |
|--------|-----|-----------|-------------|
| GET | `api/admin/reports` | Admin.ReadItems | List all reports (filter: status, entityType, entityId) |
| POST | `api/admin/reports/{reportId}/assign` | Admin.ManageItems | Assign report to admin -> UnderReview |
| POST | `api/admin/reports/{reportId}/resolve` | Admin.ManageItems | Resolve report (ActionTaken or Dismissed) |
| POST | `api/admin/reports/{reportId}/escalate-emergency` | Admin.ManageItems | Escalate auction report -> TriggerAuctionEmergency |
| GET | `api/admin/monitoring-alerts` | Admin.ReadItems | List monitoring alerts (filter: status, entityType, entityId) |
| POST | `api/admin/monitoring-alerts/{alertId}/acknowledge` | Admin.ManageItems | Acknowledge alert |
| POST | `api/admin/monitoring-alerts/{alertId}/resolve` | Admin.ManageItems | Resolve or ignore alert |
| POST | `api/admin/disputes/{disputeId}/resolve` | (Admin) | Resolve dispute with resolution type + optional escrow settlement |
| POST | `api/admin/users/{userId}/risk-flags` | Admin.ManageUsers | Flag user with risk flag |
| POST | `api/admin/auctions/{auctionId}/alerts` | Admin.ManageItems | Create monitoring alert for auction |
| POST | `api/admin/auctions/{auctionId}/bids/{bidId}/cancel` | Admin.ManageItems | Cancel an invalid bid |

## Permissions

| Permission Constant | Used By |
|---------------------|---------|
| `Admin.ManageItems` | AssignReport, ResolveReport, EscalateReportEmergency, AcknowledgeAlert, ResolveAlert, FlagAuction, CancelInvalidBid |
| `Admin.ManageUsers` | FlagUser |
| `Admin.ReadItems` | GetReports, GetMonitoringAlerts |

## Domain Events

| Event | Fields | Published By |
|-------|--------|-------------|
| `DisputeMessageSentEvent` | DisputeId, MessageId, SenderId, IsInternal, OccurredAt | SendDisputeMessageCommand handler |
| `DisputeReadStateUpdatedEvent` | DisputeId, UserId, LastReadMessageId, ReadAt | MarkDisputeReadCommand handler |
| `DisputeChangedEvent` | DisputeId, OccurredAt | ResolveDisputeCommand handler |

## SignalR Hub

**Hub path:** `/hubs/disputes` (requires authentication)

- Client-to-Server: `JoinDispute(Guid disputeId)`, `LeaveDispute(Guid disputeId)`
- Server-to-Client: `MessageReceived`, `ReadStateUpdated`, `DisputeUpdated`, `DisputeUnreadUpdated`
- Groups: `dispute:{id}`, `dispute:{id}:admins`, `dispute-user:{userId}`

See [04-dispute-chat.md](04-dispute-chat.md) for full details.

## Subflow Index

| # | File | Topic |
|---|------|-------|
| 1 | [01-create-report.md](01-create-report.md) | User creates a report |
| 2 | [02-admin-report-management.md](02-admin-report-management.md) | Admin report workflow: assign, resolve, escalate |
| 3 | [03-dispute-overview.md](03-dispute-overview.md) | Dispute entity deep-dive: child entities, enums, access control |
| 4 | [04-dispute-chat.md](04-dispute-chat.md) | Real-time dispute messaging via SignalR + REST |
