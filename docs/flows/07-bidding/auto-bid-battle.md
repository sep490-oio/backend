# Auto-Bid Battle

## Tong quan

Khi mot bid moi duoc dat (manual hoac auto), he thong tu dong trigger auto-bid cascade tu cac bidder khac. Neu nhieu auto-bidder canh tranh, he thong xu ly "auto-bid battle" - hai auto-bidder lien tuc outbid nhau cho den khi mot ben het ngan sach.

## Logic Tong quan

### ProcessAutoBids (sau moi manual bid)

```
Manual bid tu BidderA
        |
        v
ProcessAutoBids(excludeBidderId: BidderA)
        |
        v
Lay cac auto-bid eligible:
  - BidderId != BidderA
  - IsEnabled == true
  - Status == Active
  - Sap xep: MaxAmount DESC, CreatedAt ASC
        |
        v
[Duyet tung auto-bid]
        |
   [CanBid(minimumRequired)?]
        |           |
      [Co]       [Khong]
        |           |
  ProcessSingle   MarkAsOutbid
  AutoBid            (skip)
        |
   [Dat gia thanh cong?]
        |           |
      [Co]       [Khong]
        |           |
   [Chua co    (continue)
    battle?]
        |
   [BidderA co auto-bid?]
        |           |
      [Co]       [Khong]
        |           |
  ProcessAutoBid  (continue to next)
  Battle()
```

### ProcessSingleAutoBid

```
[TryConsumeAutoBidOperation(totalOps)?]
        |              |
   [OK (< 200)]    [Cap reached]
        |              |
   [CanBid?]     RaiseCascadeCapped
        |              (break)
      [Co]
        |
  CalculateNextBidAmount(minimumRequired)
        |
  PlaceAutoBidInternal(autoBid, bidAmount)
```

### ProcessAutoBidBattle

Khi auto-bid B outbid current winner va BidderA cung co auto-bid:

```
currentAttacker = BidderB (vua outbid BidderA)
currentDefender = autoBidA (auto-bid cua BidderA)

[Bat dau loop (max 100 rounds)]
        |
   [TryConsumeAutoBidOperation?]
        |           |
      [OK]       [Cap 200]
        |           |
   [Attacker     (break)
    canBid?]
        |     |
      [Co]  [Khong]
        |     |
  Calculate MarkAsOutbid
  NextBid     (break)
        |
  PlaceBid
        |
  Swap roles:
    attacker <-> defender
        |
   [New attacker canBid?]
        |           |
      [Co]       [Khong]
        |           |
  (loop tiep)  MarkAsOutbid
                   (break)
```

## Cascade Cap

- **Toi da 200 operations** per manual bid trigger
- Bao gom tat ca auto-bid operations (single + battle)
- Khi dat cap, raise `CascadeCappedEvent` (chi mot lan)
- Cac auto-bid con lai se khong duoc xu ly trong lan nay

## PlaceAutoBidInternal

Logic dat gia tu dong noi bo:

1. **Validate pricing:** `Pricing.WithNewBid(bidAmount, minimumBid)` phai thanh cong
2. **Validate budget:** `autoBid.Budget.WithBidPlaced(bidAmount)` phai thanh cong
3. **Mutate state:**
   - Mark previous winning bid as Outbid
   - Tao `Bid.Create()` voi `autoBidId` (link toi auto-bid)
   - Mark bid as Winning
   - `autoBid.UpdateCurrentAmount(bidAmount)`
   - Cap nhat Pricing, BidCount, PriceHistory
4. **Raise events:** `BidPlacedEvent` (IsAutoBid: true), `OutbidEvent`

## CalculateNextBidAmount

Auto-bid tinh toan so tien dat gia tiep theo:
- Neu co `IncrementAmount` custom: su dung increment do
- Neu khong: su dung auction's `BidIncrement`
- Gia dat = max(minimumRequired, currentPrice + increment)
- Gia dat khong vuot qua `MaxAmount`

## AutoBid Status Transitions

```
Active ──[Dat gia thanh cong]──> Active (van con budget)
Active ──[Het budget]──> Exhausted
Active ──[Bi outbid va khong du tien]──> Outbid
Active ──[Pause]──> Paused
Active ──[Auction ket thuc, thang]──> Won
Active ──[Auction ket thuc, thua]──> Lost
Paused ──[Resume]──> Active
```

## Domain Events

| Event            | Payload                                                          |
|------------------|------------------------------------------------------------------|
| `BidPlacedEvent` | AuctionId, BidId, BidderId, Amount, IsAutoBid: true, BidCount   |
| `OutbidEvent`    | AuctionId, OutbidBidderId, NewHighBidderId, NewHighestBid        |

## Luu y nghiep vu

- **Cascade cap 200:** Ngan infinite loop khi nhieu auto-bidder canh tranh. Trong thuc te, so operations thuong rat nho (2-5)
- **Battle max 100 rounds:** Safety limit cho truong hop 2 auto-bidder co maxAmount gan nhau
- **Fairness:** Auto-bid voi maxAmount cao hon va dat truoc (createAt) duoc uu tien
- **Validate truoc khi mutate:** Tat ca checks (pricing, budget) chay truoc khi thay doi state
- **Auto-bid khong trigger auto-extend:** Chi manual bid moi trigger TryAutoExtend (auto-bid internal khong goi TryAutoExtend rieng - vi no da duoc trigger boi manual bid ban dau)
- **Single-threaded:** Tat ca deu chay trong AuctionGrain -> khong co race conditions
