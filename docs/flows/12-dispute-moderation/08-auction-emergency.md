# 08 - Auction Emergency Actions

## Emergency Escalation & Bid Cancellation Flow

```mermaid
---
config:
  layout: elk
---
flowchart TD
    subgraph EscalateReport["Escalate Report to Emergency"]
        A[POST /api/admin/reports/{id}/escalate-emergency] --> B{EntityType == Auction?}
        B -->|No| ERR1[400 Report.UnsupportedEmergencyEntity]
        B -->|Yes| C[Build emergencyReason]
        C --> D[Send TriggerAuctionEmergencyCommand]
        D -->|Failure| ERR2[Return error]
        D -->|Success| E[report.MarkEscalated]
        E --> F[Status = ActionTaken\nEscalatedEmergencyAt = now]
        F --> G[Audit: report_escalated_emergency]
        G --> H[SaveChanges + return ReportDto]
    end

    subgraph TriggerEmergency["TriggerAuctionEmergencyCommand"]
        T1[Check outbound shipments] --> T2{Any picked_up / in_transit /\ndelivered / returning / returned?}
        T2 -->|Yes| TERR[EmergencyBlockedByShipment]
        T2 -->|No| T3[Load auction with emergencies + item + bids + autoBids + winnerOffers]
        T3 --> T4[auction.TriggerEmergency]
        T4 --> T5[auction.Terminate]
        T5 --> T6[Audit: auction_emergency_triggered]
        T6 --> T7{Order exists?}
        T7 -->|PendingPayment| T8[order.Cancel]
        T7 -->|Has holding escrows| T9[RefundBuyerAsync - full refund]
        T7 -->|No order| T10[Skip]
        T8 --> T11[Cancel Pending/Booked shipments]
        T9 --> T11
        T10 --> T12[Create seller UserRiskFlag\ntype: auction_emergency, High]
        T11 --> T12
        T12 --> T13{AutoSuspendOnEmergency?}
        T13 -->|Yes + seller active| T14[seller.ChangeStatus = Suspended]
        T13 -->|No| T15[Skip]
        T14 --> T16[SaveChanges]
        T15 --> T16
        T16 --> T17[Return held deposits to bidders]
    end

    subgraph CancelBid["Cancel Invalid Bid"]
        CB1[POST /api/admin/auctions/{id}/bids/{bidId}/cancel] --> CB2[Load auction with bids + priceHistories]
        CB2 --> CB3[auction.CancelBidByAdmin]
        CB3 -->|Failure| CBERR[Return error]
        CB3 -->|Success| CB4[Create MonitoringAlert\ntype: invalid_bid_cancelled\nseverity: High]
        CB4 --> CB5[Audit: invalid_bid_cancelled]
        CB5 --> CB6[SaveChanges + return BidDto]
    end
```

## Escalate Report to Emergency

```
POST /api/admin/reports/{id}/escalate-emergency
```

### Request Body

| Field          | Type    | Required |
|----------------|---------|----------|
| ReasonOverride | string? | No       |

### Validation

- `ReportId` must not be empty GUID

### Handler (`EscalateReportEmergencyCommandHandler`)

1. Loads report; returns `404 Report.NotFound` if missing
2. **Entity type check**: if `report.EntityType` is not `"Auction"` (case-insensitive) -> `400 Report.UnsupportedEmergencyEntity` with message `"Only auction reports can be escalated to emergency."`
3. Builds emergency reason:
   - If `ReasonOverride` provided and not whitespace: uses it directly
   - Otherwise: `"Escalated from report {reportId}: {report.ReasonCode}"`
4. Sends `TriggerAuctionEmergencyCommand` with:
   - `AuctionId = report.EntityId`
   - `TriggerSource = "report_escalation"`
   - `Reason = emergencyReason`
   - `Payload = report.Attachments ?? "{}"`
5. If trigger fails, returns the error
6. `report.MarkEscalated(nowUtc)`:
   - Sets `EscalatedEmergencyAt = nowUtc`
   - Sets `Status = ActionTaken`
   - Sets `ModifiedAt = nowUtc`
7. Audit log: action `report_escalated_emergency`, entityType `"Report"`, newData includes status, escalatedEmergencyAt, entityId, reason
8. SaveChanges, returns `ReportDto`

## TriggerAuctionEmergencyCommand (Internal)

This command is dispatched by `EscalateReportEmergencyCommand` and can also be invoked directly.

### Request

| Field         | Type   | Required |
|---------------|--------|----------|
| AuctionId     | Guid   | Yes      |
| TriggerSource | string | Yes      |
| Reason        | string | Yes      |
| Payload       | object | Yes      |

### Handler (`TriggerAuctionEmergencyCommandHandler`)

**Step 1 -- Shipment guard**:
- Loads order linked to auction (if any), with escrows included
- Loads outbound shipments for the order
- If any shipment has status `PickedUp`, `InTransit`, `Delivered`, `Returning`, or `Returned` -> returns `EmergencyBlockedByShipment`

**Step 2 -- Auction termination**:
- Loads auction with `Emergencies`, `Item`, `Bids`, `AutoBids`, `WinnerOffers`
- `auction.TriggerEmergency(currentUserId, triggerSource, reason, payloadJson, nowUtc)` -- creates emergency record
- `auction.Terminate(reason, nowUtc)` -- terminates the auction

**Step 3 -- Audit**:
- Action: `auction_emergency_triggered`
- EntityType: `"Auction"`
- NewData: `{ triggerSource, reason, status }`

**Step 4 -- Order cleanup** (if order exists):
- If order status is `PendingPayment`: `order.Cancel(reason, nowUtc)`
- Else if order has escrows with `Holding` status: `EscrowSettlementService.RefundBuyerAsync(order, partialAmount: null)` -- full refund
- Cancels any outbound shipments with `Pending` or `Booked` status

**Step 5 -- Seller risk flag**:
- Creates `UserRiskFlag` for the seller:
  - FlagType: `auction_emergency`
  - Severity: `High`
  - Reason: the emergency reason
  - CreatedBy: `null` (system)

**Step 6 -- Auto-suspend seller** (conditional):
- If `RuntimeSettings.Ops.AutoSuspendOnEmergency` is `true` and seller is `Active`:
  - `seller.ChangeStatus(Suspended, nowUtc)`
  - Logs warning if suspension fails

**Step 7 -- Deposit return**:
- After SaveChanges, calls `AuctionDepositReleaseDispatch.ReturnHeldDepositsAsync` to refund all held deposits to bidders

**Returns**: `AuctionEmergencyDto`

## Cancel Invalid Bid

```
POST /api/admin/auctions/{id}/bids/{bidId}/cancel
```

### Request Body

| Field  | Type    | Required |
|--------|---------|----------|
| Reason | string? | No       |

### Validation

- `AuctionId` must not be empty GUID
- `BidId` must not be empty GUID

### Handler (`CancelInvalidBidCommandHandler`)

1. Loads auction with `Bids` and `PriceHistories` included; returns `404 Auction.NotFound` if missing
2. `auction.CancelBidByAdmin(bidId, nowUtc)`:
   - Cancels the specified bid within the auction aggregate
   - Returns the cancelled bid on success
3. Creates `MonitoringAlert`:
   - EntityType: `"Auction"`
   - EntityId: auction ID
   - AlertType: `"invalid_bid_cancelled"`
   - Severity: `High`
   - Payload: `{ auctionId, bidId, reason }`
4. Audit log: action `invalid_bid_cancelled`, entityType `"Auction"`, newData: `{ auctionId, bidId, reason }`
5. SaveChanges
6. Returns `BidDto`

## Error Codes

| Code | Context |
|------|---------|
| `Report.NotFound` | Report does not exist |
| `Report.UnsupportedEmergencyEntity` | Report's EntityType is not `"Auction"` |
| `Auction.NotFound` | Auction does not exist |
| `Auction.EmergencyBlockedByShipment` | Outbound shipment already in transit/delivered/returning/returned |

## Key Source Files

| File | Path |
|------|------|
| EscalateReportEmergencyCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/EscalateReportEmergency/EscalateReportEmergencyCommand.cs` |
| TriggerAuctionEmergencyCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/TriggerAuctionEmergency/TriggerAuctionEmergencyCommand.cs` |
| CancelInvalidBidCommand | `src/core/OIO.Application/Context/ModerationContext/Commands/CancelInvalidBid/CancelInvalidBidCommand.cs` |
| Report entity | `src/core/OIO.Domain/Context/ModerationContext/Aggregates/Report.cs` |
| EscrowSettlementService | `src/core/OIO.Application/Context/OrderContext/Services/EscrowSettlementService.cs` |
