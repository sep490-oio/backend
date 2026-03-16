# SignalR

Bao gom ca hub path, negotiate endpoint, client -> server methods va server -> client events.

## AuctionHub

- URL: /hubs/auction
- Negotiate: POST /hubs/auction/negotiate?negotiateVersion=1
- Auth: Bearer token required

### Client -> Server methods

#### JoinAuction

- Muc dich: Goi realtime Join Auction tren AuctionHub.
- Audience: authenticated
- Parameters:
  - auctionId: guid (optional)
- Return: none

#### LeaveAuction

- Muc dich: Goi realtime Leave Auction tren AuctionHub.
- Audience: authenticated
- Parameters:
  - auctionId: guid (optional)
- Return: none

#### PlaceBid

- Muc dich: Goi realtime Place Bid tren AuctionHub.
- Audience: authenticated
- Permission: App.Permissions.Catalogs.Auctions.Bid
- Parameters:
  - auctionId: guid (optional)
  - amount: number (required)
  - currency: string (optional)
  - idempotencyKey: string (default = null)
- Return: [HubCommandResult<BidDto>](./schemas.md#schema-hubcommandresult-biddto)
- Notes: Method nay duoc gate boi idempotency hub filter.

#### BuyNow

- Muc dich: Goi realtime Buy Now tren AuctionHub.
- Audience: authenticated
- Permission: App.Permissions.Catalogs.Auctions.BuyNow
- Parameters:
  - auctionId: guid (optional)
- Return: [HubCommandResult<BuyNowCheckoutDto>](./schemas.md#schema-hubcommandresult-buynowcheckoutdto)

#### ConfigureAutoBid

- Muc dich: Goi realtime Configure Auto Bid tren AuctionHub.
- Audience: authenticated
- Permission: App.Permissions.Catalogs.Auctions.AutoBid
- Parameters:
  - auctionId: guid (optional)
  - maxAmount: number (required)
  - currency: string (optional)
  - incrementAmount: number (optional)
- Return: none

#### WatchAuction

- Muc dich: Goi realtime Watch Auction tren AuctionHub.
- Audience: authenticated
- Permission: App.Permissions.Catalogs.Auctions.Watch
- Parameters:
  - auctionId: guid (optional)
  - notifyOnBid: boolean (default = true)
  - notifyOnEnd: boolean (default = true)
- Return: none

### Server -> Client events

#### BidPlaced

- Muc dich: Server push Bid Placed tren AuctionHub.
- Audience: subscribers
- Payload: [BidNotification](./schemas.md#schema-bidnotification)

#### Outbid

- Muc dich: Server push Outbid tren AuctionHub.
- Audience: subscribers
- Payload: [OutbidNotification](./schemas.md#schema-outbidnotification)

#### BuyNowReserved

- Muc dich: Server push Buy Now Reserved tren AuctionHub.
- Audience: subscribers
- Payload: [BuyNowReservedNotification](./schemas.md#schema-buynowreservednotification)

#### BuyNowReservationReleased

- Muc dich: Server push Buy Now Reservation Released tren AuctionHub.
- Audience: subscribers
- Payload: [BuyNowReservationReleasedNotification](./schemas.md#schema-buynowreservationreleasednotification)

#### BuyNowExecuted

- Muc dich: Server push Buy Now Executed tren AuctionHub.
- Audience: subscribers
- Payload: [BuyNowNotification](./schemas.md#schema-buynownotification)

#### AuctionStarted

- Muc dich: Server push Auction Started tren AuctionHub.
- Audience: subscribers
- Payload: [AuctionStartedNotification](./schemas.md#schema-auctionstartednotification)

#### AuctionEnded

- Muc dich: Server push Auction Ended tren AuctionHub.
- Audience: subscribers
- Payload: [AuctionEndedNotification](./schemas.md#schema-auctionendednotification)

#### AuctionExtended

- Muc dich: Server push Auction Extended tren AuctionHub.
- Audience: subscribers
- Payload: [AuctionExtendedNotification](./schemas.md#schema-auctionextendednotification)

#### AuctionCancelled

- Muc dich: Server push Auction Cancelled tren AuctionHub.
- Audience: subscribers
- Payload: [AuctionCancelledNotification](./schemas.md#schema-auctioncancellednotification)

#### PriceUpdated

- Muc dich: Server push Price Updated tren AuctionHub.
- Audience: subscribers
- Payload: [PriceUpdateNotification](./schemas.md#schema-priceupdatenotification)
- Notes: Contract da ton tai nhung co the chua duoc publish trong runtime.

#### Error

- Muc dich: Server push Error tren AuctionHub.
- Audience: subscribers
- Payload: [ErrorNotification](./schemas.md#schema-errornotification)

## DisputeHub

- URL: /hubs/disputes
- Negotiate: POST /hubs/disputes/negotiate?negotiateVersion=1
- Auth: Bearer token required

### Client -> Server methods

#### JoinDispute

- Muc dich: Goi realtime Join Dispute tren DisputeHub.
- Audience: authenticated
- Parameters:
  - disputeId: guid (optional)
- Return: none

#### LeaveDispute

- Muc dich: Goi realtime Leave Dispute tren DisputeHub.
- Audience: authenticated
- Parameters:
  - disputeId: guid (optional)
- Return: none

### Server -> Client events

#### MessageReceived

- Muc dich: Server push Message Received tren DisputeHub.
- Audience: subscribers
- Payload: [DisputeMessageDto](./schemas.md#schema-disputemessagedto)

#### ReadStateUpdated

- Muc dich: Server push Read State Updated tren DisputeHub.
- Audience: subscribers
- Payload: [DisputeParticipantReadStateDto](./schemas.md#schema-disputeparticipantreadstatedto)

#### DisputeUpdated

- Muc dich: Server push Dispute Updated tren DisputeHub.
- Audience: subscribers
- Payload: [DisputeThreadMetaDto](./schemas.md#schema-disputethreadmetadto)

#### DisputeUnreadUpdated

- Muc dich: Server push Dispute Unread Updated tren DisputeHub.
- Audience: subscribers
- Payload: [DisputeUnreadUpdateDto](./schemas.md#schema-disputeunreadupdatedto)

## NotificationHub

- URL: /hubs/notifications
- Negotiate: POST /hubs/notifications/negotiate?negotiateVersion=1
- Auth: Bearer token required

### Client -> Server methods

### Server -> Client events

#### ReceiveNotification

- Muc dich: Server push Receive Notification tren NotificationHub.
- Audience: subscribers
- Payload: [NotificationPushDto](./schemas.md#schema-notificationpushdto)

#### UnreadCountUpdated

- Muc dich: Server push Unread Count Updated tren NotificationHub.
- Audience: subscribers
- Payload: [int](./schemas.md#schema-int)


