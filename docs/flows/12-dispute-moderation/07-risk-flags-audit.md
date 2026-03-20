# 07 - Risk Flags & Audit Logging

## User Risk Flags

### Create Risk Flag (Manual)

```
POST /api/admin/users/{id}/risk-flags
```

#### Request Body

| Field    | Type    | Required | Default    |
|----------|---------|----------|------------|
| FlagType | string  | Yes      | --         |
| Reason   | string? | No       | --         |
| Severity | string  | No       | `"medium"` |

#### Validation (`IHasValidate`)

- `UserId` must not be empty GUID
- `FlagType` must not be whitespace
- `Severity` must not be whitespace

#### Handler (`CreateUserRiskFlagCommandHandler`)

1. Loads user; returns `404 User.NotFound` if missing
2. Parses severity via `ModerationValueParsers.ParseRiskSeverity`
3. Creates `UserRiskFlag.Create(userId, flagType, reason, severity, createdBy: currentUser, nowUtc)`
4. Audit log: action `user_risk_flag_created`, entityType `"User"`, newData includes `userId`, `flagType`, `severity`, `reason`
5. SaveChanges
6. Returns `UserRiskFlagDto`

### FlagType Values (used across codebase)

| FlagType                        | Created By                              | Severity |
|---------------------------------|-----------------------------------------|----------|
| `non_payment`                   | `CancelExpiredOrdersJob` (auto)         | Medium   |
| `auction_collusion_suspected`   | `AuctionCollusionDetectionService` (auto) | Maps from alert severity (High/Critical) |
| `auction_emergency`             | `TriggerAuctionEmergencyCommand` (auto) | High     |
| `suspicious_activity`           | Admin (manual)                          | Varies   |
| `payment_fraud`                 | Admin (manual)                          | Varies   |
| `item_fraud`                    | Admin (manual)                          | Varies   |
| `seller_trust_violation`        | Admin (manual)                          | Varies   |

### RiskFlagSeverity Enum

| Value      | Id         |
|------------|------------|
| `Low`      | `low`      |
| `Medium`   | `medium`   |
| `High`     | `high`     |
| `Critical` | `critical` |

### Auto-created Risk Flags

**CancelExpiredOrdersJob**: When an order expires without payment:
- FlagType: `non_payment`
- Severity: `Medium`
- Reason: `"Order {orderNumber} expired without payment."`
- CreatedBy: `null` (system)
- Side effect: if `Ops.AutoSuspendOnNonPaymentThreshold` is configured, counts prior `non_payment` flags; if threshold reached, suspends the user

**AuctionCollusionDetectionService**: When High or Critical collusion signal detected:
- FlagType: `auction_collusion_suspected`
- Severity: mapped from `AlertSeverity` (Critical -> Critical, High -> High)
- Reason: `"Potential auction collusion detected on auction {auctionId} via {signalType}."`
- CreatedBy: `null` (system)
- Deduplication: skips if same user already has identical reason within the signal's time window

**TriggerAuctionEmergencyCommand**: When auction emergency is triggered:
- FlagType: `auction_emergency`
- Severity: `High`
- Reason: the emergency reason string
- CreatedBy: `null` (system)
- Target: seller of the auction

### UserRiskFlagDto

```csharp
public sealed record UserRiskFlagDto(
    Guid Id,
    Guid UserId,
    string FlagType,
    string? Reason,
    string Severity,
    Guid? CreatedBy,
    DateTime CreatedAt);
```

## Audit Logging

### AuditLog Entity

```csharp
public sealed class AuditLog : BaseEntity<AuditLogId>, ICreatedAtEntity
{
    public UserId? ActorUserId { get; private set; }
    public string? ActorRole { get; private set; }
    public string Action { get; private set; }
    public string EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? OldData { get; private set; }   // JSON
    public string? NewData { get; private set; }   // JSON
    public IPAddress? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
```

### ModerationAuditService.Log()

```csharp
public void Log(
    string action,
    string entityType,
    Guid? entityId,
    object? oldData = null,
    object? newData = null)
```

- Extracts `ActorUserId` from `ICurrentUser.UserId`
- Extracts `ActorRole` from the current user's claims (`ClaimTypes.Role` or `"role"`)
- Serializes `oldData` / `newData` to JSON via `JsonSerializer.Serialize`
- `IpAddress` is always `null` (not captured from HTTP context in this service)
- Inserts `AuditLog` into DbContext (saved with the surrounding unit of work)

### All Moderation Actions Creating Audit Entries

| Action String | Entity Type | Command / Source |
|---------------|-------------|------------------|
| `monitoring_alert_acknowledged` | `MonitoringAlert` | `AcknowledgeMonitoringAlertCommand` |
| `monitoring_alert_resolved` | `MonitoringAlert` | `ResolveMonitoringAlertCommand` (Ignored = false) |
| `monitoring_alert_ignored` | `MonitoringAlert` | `ResolveMonitoringAlertCommand` (Ignored = true) |
| `auction_alert_created` | `Auction` | `CreateAuctionAlertCommand` |
| `user_risk_flag_created` | `User` | `CreateUserRiskFlagCommand` |
| `invalid_bid_cancelled` | `Auction` | `CancelInvalidBidCommand` |
| `report_escalated_emergency` | `Report` | `EscalateReportEmergencyCommand` |
| `auction_emergency_triggered` | `Auction` | `TriggerAuctionEmergencyCommand` |
| `auction_emergency_resolved` | `Auction` | `ResolveAuctionEmergencyCommand` |
| `report_resolved` | `Report` | `ResolveReportCommand` |
| `report_assigned` | `Report` | `AssignReportCommand` |
| `auction_collusion_signal_detected` | `Auction` | `AuctionCollusionDetectionService` (system, no actor) |

## Key Source Files

| File | Path |
|------|------|
| UserRiskFlag entity | `src/core/OIO.Domain/Context/UserContext/Aggregates/Users/UserRiskFlag.cs` |
| CreateUserRiskFlagCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/CreateUserRiskFlag/CreateUserRiskFlagCommand.cs` |
| AuditLog entity | `src/core/OIO.Domain/Context/ModerationContext/Aggregates/AuditLog.cs` |
| ModerationAuditService | `src/core/OIO.Application/Context/ModerationContext/Services/ModerationAuditService.cs` |
| RiskFlagSeverity enum | `src/core/OIO.Domain/Context/UserContext/Enums/RiskFlagSeverity.cs` |
| CancelExpiredOrdersJob | `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Orders/CancelExpiredOrdersJob.cs` |
| AuctionCollusionDetectionService | `src/core/OIO.Application/Context/AuctionContext/Services/AuctionCollusionDetectionService.cs` |
| TriggerAuctionEmergencyCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/TriggerAuctionEmergency/TriggerAuctionEmergencyCommand.cs` |
