# 11 - Monitoring & Moderation

Admin endpoints for managing reports, monitoring alerts, user risk flags, auction alerts, bid cancellation, and dispute resolution. For detailed dispute chat and lifecycle documentation, see [Flow 12 - Dispute & Moderation](../12-dispute-moderation/README.md).

---

## Endpoints

| # | Method | Route | Description |
|---|--------|-------|-------------|
| **Reports** | | | |
| 1 | `GET` | `api/admin/reports` | List reports with filters |
| 2 | `POST` | `api/admin/reports/{reportId}/assign` | Assign report to admin |
| 3 | `POST` | `api/admin/reports/{reportId}/resolve` | Resolve or dismiss report |
| 4 | `POST` | `api/admin/reports/{reportId}/escalate-emergency` | Escalate auction report to emergency |
| **Monitoring Alerts** | | | |
| 5 | `GET` | `api/admin/monitoring-alerts` | List monitoring alerts with filters |
| 6 | `POST` | `api/admin/monitoring-alerts/{alertId}/acknowledge` | Acknowledge an alert |
| 7 | `POST` | `api/admin/monitoring-alerts/{alertId}/resolve` | Resolve or ignore an alert |
| **Risk & Moderation** | | | |
| 8 | `POST` | `api/admin/users/{userId}/risk-flags` | Create user risk flag |
| 9 | `POST` | `api/admin/auctions/{auctionId}/alerts` | Create auction monitoring alert |
| 10 | `POST` | `api/admin/auctions/{auctionId}/bids/{bidId}/cancel` | Cancel an invalid bid |
| 11 | `POST` | `api/admin/disputes/{disputeId}/resolve` | Resolve a dispute with escrow settlement |

---

## Report Lifecycle

Report statuses: `open`, `under_review`, `action_taken`, `dismissed`, `closed`.

| Transition | Trigger | Status Change |
|------------|---------|---------------|
| Assign | `AssignReportCommand` | `open` -> `under_review` |
| Resolve | `ResolveReportCommand` (dismissed=false) | any -> `action_taken` |
| Dismiss | `ResolveReportCommand` (dismissed=true) | any -> `dismissed` |
| Escalate | `EscalateReportEmergencyCommand` | any -> `action_taken` |
| Close | `Report.Close()` | any -> `closed` |

### 1. List Reports

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | `string?` | Filter by report status (case-insensitive). |
| `entityType` | `string?` | Filter by entity type (e.g., `"Auction"`, `"User"`). |
| `entityId` | `Guid?` | Filter by specific entity. |

Returns `IReadOnlyList<ReportDto>`, ordered by `CreatedAt` descending.

### 2. Assign Report

**Request Body:**

```json
{
  "assignedToUserId": "admin-guid"
}
```

Validates the assigned user exists. Sets `AssignedTo`, `AssignedAt`, and transitions status to `under_review`. Logs `"report_assigned"` via `ModerationAuditService`.

### 3. Resolve Report

**Request Body:**

```json
{
  "dismissed": false,
  "resolutionNotes": "Violation confirmed, item removed."
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `dismissed` | `bool` | Yes | `true` to dismiss, `false` to resolve with action taken. |
| `resolutionNotes` | `string?` | No | Admin notes about the resolution. |

Sets `ResolvedAt`, `ResolutionNotes`, and status to `dismissed` or `action_taken`. Sends a notification to the reporter via `NotificationDispatch` with event type `"report_dismissed"` or `"report_resolved"`. Logs via `ModerationAuditService`.

### 4. Escalate Report to Emergency

**Request Body:**

```json
{
  "reasonOverride": "Immediate action required"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `reasonOverride` | `string?` | No | Custom reason; defaults to the report's `ReasonCode`. |

**Restrictions:** Only reports with `EntityType == "Auction"` can be escalated.

**Handler Logic:**
1. Sends a `TriggerAuctionEmergencyCommand` with `TriggerSource = "report_escalation"` and the report's `EntityId` as the auction ID.
2. Marks the report as escalated (`EscalatedEmergencyAt` set, status -> `action_taken`).
3. Logs `"report_escalated_emergency"` via `ModerationAuditService`.

See [07 - Auction Emergency](./07-auction-emergency.md) for the full emergency trigger flow.

---

## Alert Lifecycle

Alert statuses: `open`, `acknowledged`, `resolved`, `ignored`.

| Transition | Trigger | Status Change |
|------------|---------|---------------|
| Acknowledge | `AcknowledgeMonitoringAlertCommand` | `open` -> `acknowledged` |
| Resolve | `ResolveMonitoringAlertCommand` (ignored=false) | any -> `resolved` |
| Ignore | `ResolveMonitoringAlertCommand` (ignored=true) | any -> `ignored` |

### 5. List Monitoring Alerts

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `status` | `string?` | Filter by alert status (case-insensitive). |
| `entityType` | `string?` | Filter by entity type. |
| `entityId` | `Guid?` | Filter by specific entity. |

Returns `IReadOnlyList<MonitoringAlertDto>`, ordered by `CreatedAt` descending.

### 6. Acknowledge Alert

**Request Body:**

```json
{
  "notes": "Investigating the issue"
}
```

Sets alert status to `acknowledged` with the current admin as the acknowledger. Logs `"monitoring_alert_acknowledged"`.

### 7. Resolve Alert

**Request Body:**

```json
{
  "ignored": false,
  "notes": "Issue confirmed and addressed"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `ignored` | `bool` | Yes | `true` to mark as ignored, `false` to resolve. |
| `notes` | `string?` | No | Resolution or ignore notes. |

Logs `"monitoring_alert_resolved"` or `"monitoring_alert_ignored"`.

---

## 8. Create User Risk Flag

**Request Body:**

```json
{
  "flagType": "suspicious_bidding",
  "reason": "Repeated bid-and-retract pattern",
  "severity": "high"
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `flagType` | `string` | Yes | -- | Type of risk flag (e.g., `"auction_emergency"`, `"suspicious_bidding"`). |
| `reason` | `string?` | No | -- | Explanation. |
| `severity` | `string` | No | `"medium"` | Parsed via `ModerationValueParsers.ParseRiskSeverity()`. |

Creates a `UserRiskFlag` entity linked to the target user and the admin who created it. Logs `"user_risk_flag_created"`.

---

## 9. Create Auction Alert

**Request Body:**

```json
{
  "alertType": "price_manipulation",
  "payload": { "details": "..." },
  "severity": "high"
}
```

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `alertType` | `string` | Yes | -- | Alert classification. |
| `payload` | `object` | Yes | -- | Arbitrary JSON data serialized and stored. |
| `severity` | `string` | No | `"medium"` | Parsed via `ModerationValueParsers.ParseAlertSeverity()`. |

Validates the auction exists. Creates a `MonitoringAlert` with `entityType: "Auction"`. Logs `"auction_alert_created"`.

---

## 10. Cancel Invalid Bid

**Route:** `POST api/admin/auctions/{auctionId}/bids/{bidId}/cancel`

**Request Body:**

```json
{
  "reason": "Collusion detected"
}
```

**Handler Logic:**
1. Loads auction with `Bids` and `PriceHistories` includes.
2. Calls `auction.CancelBidByAdmin(bidId, nowUtc)`.
3. Creates a `MonitoringAlert` with `alertType: "invalid_bid_cancelled"` and `severity: High`.
4. Logs `"invalid_bid_cancelled"` via `ModerationAuditService`.
5. Returns the cancelled `BidDto`.

---

## 11. Resolve Dispute

**Route:** `POST api/admin/disputes/{disputeId}/resolve`

**Request Body:**

```json
{
  "resolutionType": "favor_buyer",
  "notes": "Seller failed to respond within deadline",
  "amount": null
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `resolutionType` | `string` | Yes | One of the `ResolutionType` values (see below). |
| `notes` | `string?` | No | Admin resolution notes. |
| `amount` | `decimal?` | No | Partial refund amount (required for `refund_partial`). |

**Resolution Types:**

| Value | Escrow Action |
|-------|---------------|
| `favor_seller` | Release escrow to seller |
| `mutual_agreement` | Release escrow to seller |
| `favor_buyer` | Full refund to buyer |
| `refund_full` | Full refund to buyer |
| `refund_partial` | Partial refund to buyer (uses `amount` field) |
| `replacement` | No automatic escrow action |
| `no_resolution` | No automatic escrow action |
| `no_action` | No automatic escrow action |
| `cancelled` | No automatic escrow action |

**Handler Logic:**
1. Loads `Dispute` with `StatusHistory`.
2. Calls `dispute.Resolve(resolutionType, adminId, nowUtc, notes, amount)`.
3. If dispute has an associated order:
   - Marks order as disputed via `order.MarkAsDisputed()`.
   - Performs escrow settlement based on `resolutionType` via `EscrowSettlementService`.
4. Publishes `DisputeChangedEvent` for real-time updates.

See [Flow 12 - Dispute & Moderation](../12-dispute-moderation/README.md) for detailed dispute lifecycle, chat, and real-time features.

---

## Key Source Files

| File | Path |
|------|------|
| Get reports | `src/core/OIO.Application/Context/ModerationContext/Queries/GetReports/GetReportsQuery.cs` |
| Assign report | `src/core/OIO.Application/Context/ModerationContext/Commands/AssignReport/AssignReportCommand.cs` |
| Resolve report | `src/core/OIO.Application/Context/ModerationContext/Commands/ResolveReport/ResolveReportCommand.cs` |
| Escalate report | `src/core/OIO.Application/Context/ModerationContext/Commands/EscalateReportEmergency/EscalateReportEmergencyCommand.cs` |
| Get alerts | `src/core/OIO.Application/Context/ModerationContext/Queries/GetMonitoringAlerts/GetMonitoringAlertsQuery.cs` |
| Acknowledge alert | `src/core/OIO.Application/Context/ModerationContext/Commands/AcknowledgeMonitoringAlert/AcknowledgeMonitoringAlertCommand.cs` |
| Resolve alert | `src/core/OIO.Application/Context/ModerationContext/Commands/ResolveMonitoringAlert/ResolveMonitoringAlertCommand.cs` |
| Create risk flag | `src/core/OIO.Application/Context/ModerationContext/Commands/CreateUserRiskFlag/CreateUserRiskFlagCommand.cs` |
| Create auction alert | `src/core/OIO.Application/Context/ModerationContext/Commands/CreateAuctionAlert/CreateAuctionAlertCommand.cs` |
| Cancel invalid bid | `src/core/OIO.Application/Context/ModerationContext/Commands/CancelInvalidBid/CancelInvalidBidCommand.cs` |
| Resolve dispute | `src/core/OIO.Application/Context/ModerationContext/Commands/ResolveDispute/ResolveDisputeCommand.cs` |
| Report entity | `src/core/OIO.Domain/Context/ModerationContext/Aggregates/Report.cs` |
| Report status enum | `src/core/OIO.Domain/Context/ModerationContext/Enums/ReportStatus.cs` |
| Alert status enum | `src/core/OIO.Domain/Context/ModerationContext/Enums/AlertStatus.cs` |
| Resolution type enum | `src/core/OIO.Domain/Context/ModerationContext/Enums/ResolutionType.cs` |
