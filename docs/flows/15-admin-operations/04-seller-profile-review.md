# 04 - Seller Profile Review

## Overview

Admins review seller profiles to verify or reject them. Verification grants the user the `seller` role (enabling item creation, auction management, etc.). Rejection returns the profile to a state where the user can update and resubmit.

---

## Seller Profile Status Transitions

**SellerProfileStatus values:** `pending`, `verified`, `rejected`, `suspended`

| From Status | Action | To Status | Side Effect |
|---|---|---|---|
| `pending` | Verify | `verified` | Assigns `seller` role to user |
| `pending` | Reject | `rejected` | -- |
| `rejected` | (user updates & resubmits) | `pending` | -- |

---

## Trust Score Implications

Seller profile verification affects the **trust score** calculated by `SellerTrustScoreCalculator`. The trust score is a weighted composite (0-100):

| Component | Weight | Calculation |
|---|---|---|
| **Rating** | 30% | `AverageRating / 5.0 * 100` (default 50 if no reviews) |
| **Completion** | 25% | `CompletedOrders / TotalOrders * 100` (default 50 if no orders) |
| **Disputes** | 20% | `(1 - OpenDisputes / TotalOrders) * 100` (default 50 if no orders) |
| **Verification** | 15% | Approved = 100, AutoVerifyScore if available, otherwise 50 |
| **Risk** | 10% | No flags = 100, Low = 80, Medium = 50, High = 20, Critical = 0 |

An approved verification contributes a perfect 100 to the verification component (15% weight), while pending or rejected verifications contribute the `AutoVerifyScore` or a default 50.

---

## Endpoints

### 1. GET `api/admin/seller-profiles` -- List Seller Profiles

**Permission:** `admin:seller-profiles:read`

**Handler (`GetSellerProfilesQueryHandler`):**
- Queries all `SellerProfile` entities
- Orders by `CreatedAt` descending (newest first)
- Returns as `IReadOnlyCollection<SellerProfileDto>`

**Response (`SellerProfileDto`):**

| Field | Type |
|---|---|
| `Id` | Guid |
| `StoreName` | string |
| `StoreDescription` | string |
| `Status` | string (`pending`, `verified`, `rejected`, `suspended`) |
| `VerifiedAt` | DateTime? |
| `TotalSalesCount` | int |
| `TotalSalesAmount` | decimal |
| `TrustScore` | decimal |
| `TrustScoreCalculatedAt` | DateTime? |
| `CreatedAt` | DateTime |
| `ModifiedAt` | DateTime? |

---

### 2. POST `api/admin/seller-profiles/{id}/verify` -- Verify Seller Profile

**Permission:** `admin:seller-profiles:manage`

**Request (`VerifySellerProfileCommand`):** `SellerId` (Guid, from route)

**Handler logic (`VerifySellerProfileCommandHandler`):**
1. Loads `User` with `SellerProfile` included
2. Validates user exists
3. Validates seller profile exists (`SellerProfile.NotFoundById` if missing)
4. Calls `user.SellerProfile.Verify(nowUtc)` -- validates current status allows verification (`CannotVerifyInCurrentStatus` if not `pending`)
5. **Assigns `seller` role** to the user via `user.AssignRole("seller", nowUtc)`
6. Persists changes
7. Invalidates permission cache for the user (new role grants seller permissions)

---

### 3. POST `api/admin/seller-profiles/{id}/reject` -- Reject Seller Profile

**Permission:** `admin:seller-profiles:manage`

**Request (`RejectSellerProfileCommand`):** `SellerId` (Guid, from route)

**Handler logic (`RejectSellerProfileCommandHandler`):**
1. Loads `SellerProfile` by ID
2. Validates profile exists
3. Calls `profile.Reject(nowUtc)` -- validates current status allows rejection (`CannotRejectInCurrentStatus` if not `pending`)
4. Persists changes

---

## Error Codes

| Code | HTTP | Description |
|---|---|---|
| `User.NotFound` | 404 | User not found |
| `SellerProfile.NotFound` | 404 | Seller profile not found |
| `SellerProfile.CannotVerify` | 403 | Cannot verify in current status (not `pending`) |
| `SellerProfile.CannotReject` | 403 | Cannot reject in current status (not `pending`) |

---

## Source References

- `src/core/OIO.Application/Context/UserContext/Queries/GetSellerProfiles/GetSellerProfilesQuery.cs`
- `src/core/OIO.Application/Context/UserContext/Commands/VerifySellerProfile/VerifySellerProfileCommand.cs`
- `src/core/OIO.Application/Context/UserContext/Commands/RejectSellerProfile/RejectSellerProfileCommand.cs`
- `src/core/OIO.Application/Context/UserContext/Services/SellerTrustScoreCalculator.cs`
- `src/core/OIO.Application/Context/UserContext/DTOs/SellerProfileDto.cs`
- `src/core/OIO.Domain/Context/UserContext/Enums/SellerProfileStatus.cs`
