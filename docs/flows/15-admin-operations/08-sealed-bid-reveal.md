# 08 - Sealed Bid Reveal

Admin endpoint to decrypt and reveal a sealed bid's amount after a sealed auction has ended.

---

## Endpoint

| Method | Route | Permission |
|--------|-------|------------|
| `POST` | `api/admin/auctions/{auctionId}/sealed-bids/{sealedBidId}/reveal` | `Catalogs.Admin.ManageItems` |

No request body required. The `auctionId` and `sealedBidId` are taken from the route.

---

## Handler Logic

**Source:** `AdminRevealSealedBidCommandHandler`

1. **Load auction** by `AuctionId` with `SealedBids` include.
2. **Call `auction.RevealSealedBid(sealedBidId, currentUserId, nowUtc)`.**
3. **Save changes** and return the `SealedBidDto`.

---

## Domain Validation (`Auction.RevealSealedBid`)

The method performs the following checks:

| Check | Error |
|-------|-------|
| Auction type must be `Sealed` | `AuctionErrors.SealedBid.OnlySupportedForSealedAuction` |
| Auction must not be `Active` and must have ended (`Info.HasEnded(nowUtc)`) | `AuctionErrors.SealedBid.RevealNotAllowed` |
| Sealed bid must exist by ID in the auction's `_sealedBids` collection | `AuctionErrors.SealedBid.NotFound` |

When accepted:
- Calls `sealedBid.Reveal(actorId, nowUtc)` which sets `Status` to `Revealed`, `RevealedAt` to `nowUtc`, and `RevealedBy` to the admin's `UserId`.
- Sets `auction.ModifiedAt` to `nowUtc`.
- Returns the revealed `SealedBid` entity.

---

## SealedBid Entity

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `SealedBidId` | Unique identifier. |
| `AuctionId` | `AuctionId` | Parent auction. |
| `BidderId` | `UserId` | User who submitted the sealed bid. |
| `AmountEncrypted` | `string` | Encrypted bid amount (ciphertext). |
| `Status` | `SealedBidStatus` | `Submitted` or `Revealed`. |
| `CreatedAt` | `DateTime` | When the bid was submitted. |
| `RevealedAt` | `DateTime?` | When the bid was revealed. |
| `RevealedBy` | `UserId?` | Admin who performed the reveal. |

---

## ISealedBidEncryptionService

The encryption service interface used elsewhere for bulk reveal operations:

```csharp
public interface ISealedBidEncryptionService
{
    string Encrypt(decimal amount);
    Result<decimal, Error> Decrypt(string encryptedAmount);
}
```

- `Encrypt` converts a decimal amount to an encrypted string (used during bid submission).
- `Decrypt` converts the encrypted string back to a decimal amount (used during bulk reveal in `RevealAllSealedBids`).

Note: The single-bid admin reveal endpoint (`AdminRevealSealedBidCommand`) marks the bid as revealed at the domain level. The `RevealedAmount` field in the DTO is populated via the `ToDto()` mapping, which accepts an optional `revealedAmount` parameter.

---

## Response DTO

```json
{
  "id": "guid",
  "auctionId": "guid",
  "bidderId": "guid",
  "amountEncrypted": "encrypted-string",
  "status": "revealed",
  "createdAt": "2026-03-20T10:00:00Z",
  "revealedAt": "2026-03-21T14:30:00Z",
  "revealedBy": "admin-guid",
  "revealedAmount": null
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Sealed bid ID. |
| `auctionId` | `Guid` | Parent auction ID. |
| `bidderId` | `Guid` | Bidder user ID. |
| `amountEncrypted` | `string` | The encrypted bid amount. |
| `status` | `string` | `"submitted"` or `"revealed"`. |
| `createdAt` | `DateTime` | Submission timestamp. |
| `revealedAt` | `DateTime?` | Reveal timestamp. |
| `revealedBy` | `Guid?` | Admin user ID who revealed. |
| `revealedAmount` | `decimal?` | Decrypted amount (populated when decryption service is used in bulk reveal). |

---

## Key Source Files

| File | Path |
|------|------|
| Command + Handler | `src/core/OIO.Application/Context/AuctionContext/Commands/AdminRevealSealedBid/AdminRevealSealedBidCommand.cs` |
| Domain method | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` (`RevealSealedBid`) |
| SealedBid entity | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/SealedBid.cs` |
| Encryption service | `src/core/OIO.Application/Context/AuctionContext/Services/ISealedBidEncryptionService.cs` |
| DTO mapping | `src/core/OIO.Application/Context/AuctionContext/Mappings/AuctionSupportMappings.cs` |
