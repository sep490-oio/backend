# 11 - Background Jobs

## Overview

The auction lifecycle relies on five background jobs that handle time-based state transitions,
safety-net polling, post-sale completion, and runner-up offer expiration.
Three are Quartz.NET scheduled jobs; one is a Quartz.NET polling job; one is a .NET `BackgroundService`.

---

## Job Summary

| Job | Type | Schedule | Group | Source |
|---|---|---|---|---|
| `ActivateAuctionJob` | Quartz per-auction timer | Fires once at `startTime` | `auction-lifecycle` | Scheduled by `QuartzAuctionScheduler.ScheduleStartAsync` |
| `EndAuctionJob` | Quartz per-auction timer | Fires once at `endTime` | `auction-lifecycle` | Scheduled by `QuartzAuctionScheduler.ScheduleEndAsync` |
| `AuctionPollingFallbackJob` | Quartz recurring | Every 60 seconds | `system` | Registered in `AuctionJobSetup` |
| `AuctionAutoCompleteJob` | Quartz recurring | Every 1 hour | `system` | Registered in `AuctionAutoCompleteJobSetup` |
| `ExpireRunnerUpOffersJob` | .NET `BackgroundService` | Every 5 minutes | N/A (hosted service) | Registered via `AddHostedService` |

---

## Flowchart

```mermaid
---
config:
  layout: elk
---
flowchart TB
    subgraph PerAuctionTimers["Per-Auction Timers (Quartz, auction-lifecycle group)"]
        direction TB

        SCHEDULE["Auction Published / Scheduled"]
        ACTIVATE_JOB["ActivateAuctionJob<br/>Fires once at startTime<br/>Sends ActivateAuctionCommand"]
        END_JOB["EndAuctionJob<br/>Fires once at endTime<br/>Sends EndAuctionCommand"]

        SCHEDULE -->|"ScheduleStartAsync(auctionId, startTime)"| ACTIVATE_JOB
        SCHEDULE -->|"ScheduleEndAsync(auctionId, endTime)"| END_JOB
        ACTIVATE_JOB -->|"Auction status: Scheduled -> Active"| END_JOB
    end

    subgraph FallbackPolling["Safety-Net Polling (Quartz, system group)"]
        direction TB

        POLL_TRIGGER["SimpleSchedule<br/>Every 60 seconds<br/>RepeatForever"]
        POLL_JOB["AuctionPollingFallbackJob"]
        OVERDUE_STARTS["Query: Scheduled auctions<br/>where StartTime <= now"]
        OVERDUE_ENDS["Query: Active auctions<br/>where EndTime <= now"]

        POLL_TRIGGER --> POLL_JOB
        POLL_JOB --> OVERDUE_STARTS
        POLL_JOB --> OVERDUE_ENDS
        OVERDUE_STARTS -->|"Send ActivateAuctionCommand"| ACTIVATE_FALLBACK["Activate overdue auctions"]
        OVERDUE_ENDS -->|"Send EndAuctionCommand"| END_FALLBACK["End overdue auctions"]
    end

    subgraph AutoComplete["Post-Sale Completion (Quartz, system group)"]
        direction TB

        AC_TRIGGER["SimpleSchedule<br/>Every 1 hour<br/>RepeatForever"]
        AC_JOB["AuctionAutoCompleteJob"]
        AC_QUERY["Query: Sold auctions<br/>with delivered outbound shipment<br/>DeliveredAt <= now - 3 days"]
        AC_DISPUTE["Check: No open disputes<br/>(not Resolved/Closed/Cancelled)"]
        AC_COMPLETE["Item.MarkSold(now)<br/>Trigger seller payment"]

        AC_TRIGGER --> AC_JOB
        AC_JOB --> AC_QUERY
        AC_QUERY --> AC_DISPUTE
        AC_DISPUTE -->|"No disputes"| AC_COMPLETE
        AC_DISPUTE -->|"Has open dispute"| AC_SKIP["Skip auction"]
    end

    subgraph RunnerUp["Runner-Up Offer Expiration (BackgroundService)"]
        direction TB

        RU_TRIGGER["Task.Delay loop<br/>Every 5 minutes"]
        RU_JOB["ExpireRunnerUpOffersJob"]
        RU_QUERY["Query: PaymentDefaulted auctions<br/>with Pending offers<br/>where ExpiresAt < now<br/>Take(50)"]
        RU_EXPIRE["auction.ExpirePendingWinnerOffers(now)"]
        RU_NOTIFY_BIDDER["Notify bidder:<br/>runner_up_offer_expired"]
        RU_NOTIFY_SELLER["Notify seller:<br/>runner_up_offer_expired<br/>Actions: offer_next_rank, relist"]

        RU_TRIGGER --> RU_JOB
        RU_JOB --> RU_QUERY
        RU_QUERY --> RU_EXPIRE
        RU_EXPIRE --> RU_NOTIFY_BIDDER
        RU_EXPIRE --> RU_NOTIFY_SELLER
    end

    PerAuctionTimers -.->|"Missed timer?<br/>Fallback catches it"| FallbackPolling
    END_JOB -->|"Auction ends with winner<br/>status -> Sold"| AutoComplete
    END_JOB -->|"Winner defaults on payment<br/>status -> PaymentDefaulted"| RunnerUp
```

---

## Job Details

### 1. ActivateAuctionJob

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Auctions/ActivateAuctionJob.cs`
- **Type:** Quartz `IJob`, `[DisallowConcurrentExecution]`
- **Group:** `auction-lifecycle`
- **Trigger:** One-shot timer scheduled by `QuartzAuctionScheduler.ScheduleStartAsync` when an auction is published/scheduled. Fires at the auction's `startTime`.
- **Behavior:** Reads `AuctionId` from `MergedJobDataMap`, sends `ActivateAuctionCommand` via MediatR. Logs error if the command fails.
- **Idempotent:** Yes -- if the auction is already active, the command is a no-op.
- **Misfire policy:** `WithMisfireHandlingInstructionFireNow` -- fires immediately if the scheduler was down at the scheduled time.

### 2. EndAuctionJob

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Auctions/EndAuctionJob.cs`
- **Type:** Quartz `IJob`, `[DisallowConcurrentExecution]`
- **Group:** `auction-lifecycle`
- **Trigger:** One-shot timer scheduled by `QuartzAuctionScheduler.ScheduleEndAsync`. Fires at the auction's `endTime`. Can be rescheduled via `RescheduleEndAsync` (used by auto-extension / anti-sniping).
- **Behavior:** Reads `AuctionId` from `MergedJobDataMap`, sends `EndAuctionCommand` via MediatR.
- **Misfire policy:** `WithMisfireHandlingInstructionFireNow`.

### 3. AuctionPollingFallbackJob

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Auctions/AuctionPollingFallbackJob.cs`
- **Type:** Quartz `IJob`, `[DisallowConcurrentExecution]`
- **Group:** `system`
- **Schedule:** `WithSimpleSchedule` every 60 seconds, `RepeatForever`, `WithMisfireHandlingInstructionFireNow`.
- **Purpose:** Safety net. In normal operation this job finds nothing to do. It catches auctions whose per-auction timers failed (timer loss, race condition, scheduler restart).
- **Behavior:**
  1. Queries `Scheduled` auctions where `StartTime <= now` and sends `ActivateAuctionCommand` for each.
  2. Queries `Active` auctions where `EndTime <= now` and sends `EndAuctionCommand` for each.
  3. Logs a warning with counts if any overdue auctions were processed.

### 4. AuctionAutoCompleteJob

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/AuctionAutoCompleteJob.cs`
- **Type:** Quartz `IJob`, `[DisallowConcurrentExecution]`
- **Group:** `system`
- **Schedule:** `WithSimpleSchedule` every 1 hour, `RepeatForever`, `WithMisfireHandlingInstructionFireNow`.
- **Purpose:** Automatically completes sold auctions after delivery confirmation + grace period.
- **Behavior:**
  1. Computes cutoff = `now - AutoCompleteDaysAfterDelivery` (3 days).
  2. Loads all `Sold` auctions (with `Item` included).
  3. For each, checks if an `OutboundShipment` exists with `Status == Delivered` and `DeliveredAt <= cutoff`.
  4. Checks no open `Dispute` exists (status not `Resolved`, `Closed`, or `Cancelled`).
  5. Calls `auction.Item.MarkSold(now)` and saves changes.

### 5. ExpireRunnerUpOffersJob

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/Jobs/Auctions/ExpireRunnerUpOffersJob.cs`
- **Type:** .NET `BackgroundService` (hosted service)
- **Schedule:** `Task.Delay` loop every 5 minutes.
- **Purpose:** Expires pending runner-up winner offers that have passed their `ExpiresAt` deadline.
- **Behavior:**
  1. Queries up to 50 `PaymentDefaulted` auctions that have `Pending` winner offers with `ExpiresAt < now`.
  2. Calls `auction.ExpirePendingWinnerOffers(now)` on each.
  3. Sends `CreateNotificationCommand` to each expired bidder (`runner_up_offer_expired`).
  4. Sends `CreateNotificationCommand` to the seller with actionable buttons: "offer next rank" and "relist".

---

## Scheduling Infrastructure

### QuartzAuctionScheduler

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/QuartzAuctionScheduler.cs`
- Implements `IAuctionScheduler` interface.
- Methods:
  - `ScheduleStartAsync(auctionId, startTime)` -- creates an `ActivateAuctionJob` one-shot trigger.
  - `ScheduleEndAsync(auctionId, endTime)` -- creates an `EndAuctionJob` one-shot trigger.
  - `RescheduleEndAsync(auctionId, newEndTime)` -- reschedules the end trigger (for auto-extension). Falls back to `ScheduleEndAsync` if the trigger does not exist.
  - `CancelAsync(auctionId)` -- deletes both start and end jobs (used on auction cancellation).
- All methods are idempotent: they delete existing jobs before re-scheduling.
- If `startTime` or `endTime` is in the past, the trigger fires 1 second from now.

### Job Constants

- **File:** `src/infrastructure/OIO.Infrastructure/Scheduling/JobConstants.cs`
- `SystemGroup = "system"` -- for recurring polling/batch jobs.
- `AuctionLifecycleGroup = "auction-lifecycle"` -- for per-auction one-shot timers.
