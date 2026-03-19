# Late Payment (Thanh toan muon)

## Tong quan

Truong hop dac biet: reservation da het han (15 phut) nhung VNPay callback bao thanh toan thanh cong. Day co the xay ra do do tre mang, VNPay xu ly cham, hoac nguoi dung thanh toan sat thoi diem het han.

## Actors

- **System:** Xu ly payment callback sau khi reservation het han
- **Buyer (Nguoi mua):** Nhan tien credit vao wallet

## Scenario

```
T+0:00  Buyer tao reservation (15 min window)
T+0:05  Buyer bat dau thanh toan tren VNPay
T+15:00 Reservation het han
T+15:01 ExpireBuyNowReservationsJob danh dau reservation Expired
T+15:30 VNPay callback thanh cong
        |
        v
   [Reservation con active?]
        |
   [Khong - da Expired]
        |
   FinalizeBuyNowReservation -> Error: Expired
        |
   Credit tien vao wallet cua buyer
```

## Xu ly

Khi VNPay callback thanh cong nhung reservation da het han:

1. `auction.FinalizeBuyNowReservation()` tra ve loi `BuyNowReservation.Expired`
2. He thong khong finalize reservation (khong chuyen auction sang Sold)
3. **So tien da thanh toan duoc credit vao wallet cua buyer**
4. Buyer co the su dung so du wallet cho cac giao dich khac

## Logic Chi tiet

```
VNPay IPN callback (thanh cong)
        |
        v
Identify: purpose = auction_buy_now
        |
        v
Tim reservation theo reservationId
        |
        v
[reservation.IsActive(nowUtc)?]
        |              |
      [Co]          [Khong - da Expired/Failed]
        |              |
  Finalize        Credit wallet
  (binh thuong)    Amount = gatewayAmountDue
        |              |
   Auction Sold     Buyer nhan tien
                    trong wallet
```

## Luu y nghiep vu

- Day la truong hop edge case nhung quan trong - khong the de mat tien cua buyer
- Tien duoc credit vao wallet (khong phai hoan tien ve VNPay) vi nhanh hon va giam phi giao dich
- Buyer co the dung wallet balance cho phien dau gia khac hoac rut tien
- He thong ghi audit log cho truong hop late payment de tracking
- Reservation da Expired khong the duoc finalize - dam bao trang thai nhat quan
- Auction co the da co nguoi thang khac (neu chuyen sang Active va co bidder thang)
- Neu auction van Scheduled sau khi reservation expire, buyer co the thu lai voi reservation moi
