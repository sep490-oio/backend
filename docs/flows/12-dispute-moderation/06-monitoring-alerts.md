# 06 - Monitoring Alerts

## Alert Lifecycle

```mermaid
flowchart TD
    subgraph AutoSources["Auto-created Alerts"]
        COL[ScanActiveAuctionsForCollusionJob\nevery 5 min\nAlertTypes: seller_bidder_same_device,\nseller_bidder_same_ip, top_bidders_same_device,\ntop_bidders_same_ip, ping_pong_bidding,\nrepeated_pair]
        BF[LoginAttemptedEventHandler\nAlertType: login_brute_force\nSeverity: High\nTrigger: 5 failed attempts/hour/IP]
        NP[CancelExpiredOrdersJob\nAlertType: repeated_non_payment\nSeverity: Medium]
        SUS[LoginAttemptedEventHandler\nAlertType: suspicious_login_new_ip\nSeverity: Medium]
    end

    subgraph ManualSource["Manual Alert"]
        MAN[POST /api/admin/auctions/{id}/alerts\nAlertType + Payload + Severity]
    end

    COL --> OPEN[MonitoringAlert\nStatus: Open]
    BF --> OPEN
    NP --> OPEN
    SUS --> OPEN
    MAN --> OPEN

    OPEN --> ACK[POST .../acknowledge\nStatus: Acknowledged\nAcknowledgedBy + AcknowledgedAt + Notes]
    ACK --> RES{Resolve or Ignore?}
    RES -->|Ignored = false| RESOLVED[POST .../resolve\nStatus: Resolved\nResolvedBy + ResolvedAt + Notes]
    RES -->|Ignored = true| IGNORED[POST .../resolve\nStatus: Ignored\nResolvedBy + ResolvedAt + Notes]
```

## Auto-created Alert Sources

### 1. ScanActiveAuctionsForCollusionJob

- **Schedule**: Runs every 5 minutes (`BackgroundService` loop)
- **Scope**: Queries up to 100 active or scheduled auctions (ordered by newest)
- **Detection service**: `IAuctionCollusionDetectionService.ScanAuctionAsync`
- **Signal types and alert types**:

| AlertType | Signal | Severity | Score | Condition |
|-----------|--------|----------|-------|-----------|
| `auction_collusion_seller_bidder_same_device` | `seller_bidder_same_device_recent` | Critical | 100 | Seller and bidder used same device within configurable window |
| `auction_collusion_seller_bidder_same_ip` | `seller_bidder_same_ip_recent` | High | 85 | Seller and bidder used same IP within configurable window |
| `auction_collusion_top_bidders_same_device` | `top_bidders_same_device_recent` | High | 80 | Top 2 bidders used same device |
| `auction_collusion_top_bidders_same_ip` | `top_bidders_same_ip_recent` | Medium | 65 | Top 2 bidders used same IP or same bid IP |
| `auction_collusion_ping_pong_bidding` | `ping_pong_bid_ladder` | Medium | 60 | Two bidders dominate recent bids with alternating pattern |
| `auction_collusion_repeated_pair` | `repeated_suspicious_pair_recent` | High | 90 | Same pair triggered strong signals in multiple auctions |

- **Deduplication**: Skips alert if an open alert of the same type exists for the same auction within the signal's window
- **Side effects**: Creates `UserRiskFlag` (type `auction_collusion_suspected`) for implicated users when severity is High or Critical; creates `AuditLog` entries with `actorRole: "system"`, action `auction_collusion_signal_detected`

### 2. LoginAttemptedEventHandler

Handles `LoginAttemptedEvent` domain event.

**Brute force detection** (`login_brute_force`):
- Tracks failed login count per IP in `HybridCache` with 1-hour TTL
- When count reaches threshold (`FailedAttemptThreshold = 5`), creates alert:
  - EntityType: `User`, EntityId: userId
  - AlertType: `login_brute_force`
  - Severity: `High`
  - Payload: `{ ipAddress, failedAttempts, windowHours }`

**Suspicious login detection** (`suspicious_login_new_ip`):
- On successful login, checks if IP is known (cached known IPs, falling back to `UserLoginHistory` query)
- If new IP detected, creates alert:
  - EntityType: `User`, EntityId: userId
  - AlertType: `suspicious_login_new_ip`
  - Severity: `Medium`
  - Payload: `{ ipAddress, userAgent, occurredAt }`
- Also sends in-app notification (type `security`, priority `High`)

### 3. CancelExpiredOrdersJob

- When an order expires without payment, creates:
  - `UserRiskFlag` (type `non_payment`, severity `Medium`)
  - `MonitoringAlert`:
    - EntityType: `User`, EntityId: buyerId
    - AlertType: `repeated_non_payment`
    - Severity: `Medium`
    - Payload: `{ userId, orderId, orderNumber, cancelledOrders }`

## Manual Alert Creation

```
POST /api/admin/auctions/{id}/alerts
```

### Request Body

| Field     | Type   | Required | Default    |
|-----------|--------|----------|------------|
| AlertType | string | Yes      | --         |
| Payload   | object | Yes      | --         |
| Severity  | string | No       | `"medium"` |

### Handler (`CreateAuctionAlertCommandHandler`)

1. Verifies auction exists; returns `404 Auction.NotFound` if not
2. Parses severity via `ModerationValueParsers.ParseAlertSeverity`
3. Creates `MonitoringAlert` with EntityType `"Auction"`, EntityId = auctionId
4. Serializes `Payload` to JSON
5. Audit log: action `auction_alert_created`
6. Returns `MonitoringAlertDto`

## List Alerts

```
GET /api/admin/monitoring-alerts
```

### Query Parameters

| Parameter  | Type   | Description                          |
|------------|--------|--------------------------------------|
| Status     | string | Filter by alert status (e.g. `open`) |
| EntityType | string | Filter by entity type                |
| EntityId   | Guid   | Filter by entity ID                  |

Results ordered by `CreatedAt` descending.

## Acknowledge Alert

```
POST /api/admin/monitoring-alerts/{id}/acknowledge
```

### Request Body

| Field | Type    | Required |
|-------|---------|----------|
| Notes | string? | No      |

### Handler (`AcknowledgeMonitoringAlertCommandHandler`)

1. Loads alert; returns `404 MonitoringAlert.NotFound` if missing
2. Captures old state (`status`, `notes`)
3. `alert.Acknowledge(adminId, notes, nowUtc)`:
   - Sets `Status = Acknowledged`, `AcknowledgedBy`, `AcknowledgedAt`, `Notes`
4. Audit log: action `monitoring_alert_acknowledged`, with old/new state
5. SaveChanges
6. Returns `MonitoringAlertDto`

## Resolve Alert

```
POST /api/admin/monitoring-alerts/{id}/resolve
```

### Request Body

| Field   | Type    | Required |
|---------|---------|----------|
| Ignored | bool    | Yes      |
| Notes   | string? | No       |

### Handler (`ResolveMonitoringAlertCommandHandler`)

1. Loads alert; returns `404 MonitoringAlert.NotFound` if missing
2. Captures old state (`status`, `notes`)
3. `alert.Resolve(adminId, notes, ignored, nowUtc)`:
   - If `Ignored = true`: `Status = Ignored`
   - If `Ignored = false`: `Status = Resolved`
   - Sets `ResolvedBy`, `ResolvedAt`, `Notes`
4. Audit log: action `monitoring_alert_resolved` or `monitoring_alert_ignored`
5. SaveChanges
6. Returns `MonitoringAlertDto`

## Enums

### AlertSeverity

| Value      | Id         |
|------------|------------|
| `Low`      | `low`      |
| `Medium`   | `medium`   |
| `High`     | `high`     |
| `Critical` | `critical` |

### AlertStatus

| Value          | Id             |
|----------------|----------------|
| `Open`         | `open`         |
| `Acknowledged` | `acknowledged` |
| `Resolved`     | `resolved`     |
| `Ignored`      | `ignored`      |

## MonitoringAlertDto

```csharp
public sealed record MonitoringAlertDto(
    Guid Id,
    string EntityType,
    Guid EntityId,
    string AlertType,
    string Severity,
    string Payload,       // JSON string
    string Status,
    string? Notes,
    Guid? AcknowledgedBy,
    DateTime? AcknowledgedAt,
    Guid? ResolvedBy,
    DateTime? ResolvedAt,
    DateTime CreatedAt);
```

## Key Source Files

| File | Path |
|------|------|
| MonitoringAlert entity | `src/core/OIO.Domain/Context/ModerationContext/Aggregates/MonitoringAlert.cs` |
| AcknowledgeMonitoringAlertCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/AcknowledgeMonitoringAlert/AcknowledgeMonitoringAlertCommand.cs` |
| ResolveMonitoringAlertCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/ResolveMonitoringAlert/ResolveMonitoringAlertCommand.cs` |
| CreateAuctionAlertCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/CreateAuctionAlert/CreateAuctionAlertCommand.cs` |
| GetMonitoringAlertsQuery | `src/core/OIO.Application/Context/ModerationContext/Queries/GetMonitoringAlerts/GetMonitoringAlertsQuery.cs` |
| ScanActiveAuctionsForCollusionJob | `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Auctions/ScanActiveAuctionsForCollusionJob.cs` |
| AuctionCollusionDetectionService | `src/core/OIO.Application/Context/AuctionContext/Services/AuctionCollusionDetectionService.cs` |
| LoginAttemptedEventHandler | `src/core/OIO.Application/Context/UserContext/EventHandlers/LoginAttemptedEventHandler.cs` |
| CancelExpiredOrdersJob | `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Orders/CancelExpiredOrdersJob.cs` |
| AlertSeverity enum | `src/core/OIO.Domain/Context/ModerationContext/Enums/AlertSeverity.cs` |
| AlertStatus enum | `src/core/OIO.Domain/Context/ModerationContext/Enums/AlertStatus.cs` |
