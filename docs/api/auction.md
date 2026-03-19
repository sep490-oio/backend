# Auction + Catalog

Bao gom Categories, Items, Sellers va Auctions.

### GET /api/auctions

- Muc dich: Lay Get Auctions qua GET /api/auctions.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: [GetAuctionsEndpoint.Parameters](./schemas.md#schema-getauctionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<AuctionListItemDto>](./schemas.md#schema-pagedlist-auctionlistitemdto)
- Error statuses: none documented

### POST /api/auctions

- Muc dich: Thuc hien Create Auction qua POST /api/auctions.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Create
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateAuctionEndpoint.Request](./schemas.md#schema-createauctionendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### GET /api/auctions/{auctionId:guid}

- Muc dich: Lay Get Auction By Id qua GET /api/auctions/{auctionId:guid}.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found
- Notes:
  - `priceHistory` trong chi tiet auction se tra them truong `type` de phan biet nguon goc gia nhu `starting_price`, `bid`, `buy_now`, `sealed_bid`, `reset_to_starting_price`, `repriced_after_bid_cancellation`.

### PUT /api/auctions/{auctionId:guid}

- Muc dich: Cap nhat Update Auction qua PUT /api/auctions/{auctionId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Create
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [UpdateAuctionEndpoint.Request](./schemas.md#schema-updateauctionendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### PUT /api/auctions/{auctionId:guid}/auto-bid

- Muc dich: Cap nhat Configure Auto Bid qua PUT /api/auctions/{auctionId:guid}/auto-bid.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.AutoBid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [ConfigureAutoBidEndpoint.Request](./schemas.md#schema-configureautobidendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### GET /api/auctions/{auctionId:guid}/auto-bid/my

- Muc dich: Lay Get My Auto Bid qua GET /api/auctions/{auctionId:guid}/auto-bid/my.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found

### POST /api/auctions/{auctionId:guid}/auto-bid/pause

- Muc dich: Thuc hien Pause Auto Bid qua POST /api/auctions/{auctionId:guid}/auto-bid/pause.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.AutoBid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### POST /api/auctions/{auctionId:guid}/auto-bid/resume

- Muc dich: Thuc hien Resume Auto Bid qua POST /api/auctions/{auctionId:guid}/auto-bid/resume.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.AutoBid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/auctions/{auctionId:guid}/bids

- Muc dich: Lay Get Auction Bids qua GET /api/auctions/{auctionId:guid}/bids.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.ReadAutoBid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: [GetAuctionBidsEndpoint.Parameters](./schemas.md#schema-getauctionbidsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found

### POST /api/auctions/{auctionId:guid}/bids

- Muc dich: Thuc hien Place Bid qua POST /api/auctions/{auctionId:guid}/bids.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Bid
- Headers: Authorization: Bearer <token>, Idempotency-Key: <unique-key> (required)
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [PlaceBidEndpoint.Request](./schemas.md#schema-placebidendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request, 409 Conflict, 422 Unprocessable Entity
- Notes:
  - Endpoint nay duoc gate boi Idempotency filter.

### POST /api/auctions/{auctionId:guid}/buy-now

- Muc dich: Thuc hien Buy Now qua POST /api/auctions/{auctionId:guid}/buy-now.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.BuyNow
- Headers: Authorization: Bearer <token>, Idempotency-Key: <unique-key> (required)
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request, 409 Conflict
- Notes:
  - Endpoint nay duoc gate boi Idempotency filter.

### POST /api/auctions/{auctionId:guid}/cancel

- Muc dich: Thuc hien Cancel Auction qua POST /api/auctions/{auctionId:guid}/cancel.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Cancel
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [CancelAuctionEndpoint.Request](./schemas.md#schema-cancelauctionendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/auctions/{auctionId:guid}/close

- Muc dich: Thuc hien Close Auction qua POST /api/auctions/{auctionId:guid}/close.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Cancel
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/auctions/{auctionId:guid}/publish

- Muc dich: Thuc hien Publish Auction qua POST /api/auctions/{auctionId:guid}/publish.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Publish
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request
- Notes:
  - Neu publish khi auction da den `StartAt`, he thong se thu activate ngay trong cung request.
  - Auction chi duoc bat dau khi co it nhat 1 participant du dieu kien bid: `Qualified`, chua `Withdrawn`, va con deposit `Held`.
  - Neu khong co participant du dieu kien o thoi diem start, auction se tu `Cancelled` va item duoc tra ve `Active`.

### POST /api/auctions/{auctionId:guid}/relist

- Muc dich: Thuc hien Relist Auction qua POST /api/auctions/{auctionId:guid}/relist.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Submit
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [RelistAuctionEndpoint.Request](./schemas.md#schema-relistauctionendpoint-request)
- Success responses:
  - 200 OK: [AuctionDto](./schemas.md#schema-auctiondto)
- Error statuses: 400 Bad Request, 403 Forbidden, 404 Not Found, 409 Conflict
- Notes:
  - Co the override cac gia tri pricing cho lan relist moi: `StartingPrice`, `BidIncrement`, `ReservePrice`, `BuyNowPrice`, `Currency`.
  - Neu khong truyen pricing override, he thong se copy pricing tu auction cu.

### POST /api/auctions/{auctionId:guid}/runner-up-offers

- Muc dich: Thuc hien Offer Runner Up qua POST /api/auctions/{auctionId:guid}/runner-up-offers.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Submit
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [WinnerOfferDto](./schemas.md#schema-winnerofferdto)
- Error statuses: 400 Bad Request, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/auctions/{auctionId:guid}/runner-up-offers/respond

- Muc dich: Thuc hien Respond Runner Up Offer qua POST /api/auctions/{auctionId:guid}/runner-up-offers/respond.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Bid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [RespondRunnerUpOfferEndpoint.Request](./schemas.md#schema-respondrunnerupofferendpoint-request)
- Success responses:
  - 200 OK: [WinnerOfferDto](./schemas.md#schema-winnerofferdto)
- Error statuses: 400 Bad Request, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/auctions/{auctionId:guid}/sealed-bids

- Muc dich: Thuc hien Submit Sealed Bid qua POST /api/auctions/{auctionId:guid}/sealed-bids.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Bid
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [SubmitSealedBidEndpoint.Request](./schemas.md#schema-submitsealedbidendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### POST /api/auctions/{auctionId:guid}/shipping

- Muc dich: Thuc hien Choose Auction Shipping qua POST /api/auctions/{auctionId:guid}/shipping.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Submit
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [ChooseAuctionShippingEndpoint.Request](./schemas.md#schema-chooseauctionshippingendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### POST /api/auctions/{auctionId:guid}/submit

- Muc dich: Thuc hien Submit Auction qua POST /api/auctions/{auctionId:guid}/submit.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Submit
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### PUT /api/auctions/{auctionId:guid}/timing

- Muc dich: Cap nhat Set Auction Timing qua PUT /api/auctions/{auctionId:guid}/timing.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Create
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: [SetAuctionTimingEndpoint.Request](./schemas.md#schema-setauctiontimingendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### DELETE /api/auctions/{auctionId:guid}/watch

- Muc dich: Xoa Unwatch Auction qua DELETE /api/auctions/{auctionId:guid}/watch.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Unwatch
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/auctions/{auctionId:guid}/watch

- Muc dich: Thuc hien Watch Auction qua POST /api/auctions/{auctionId:guid}/watch.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Watch
- Headers: Authorization: Bearer <token>
- Path params:
  - auctionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 409 Conflict

### GET /api/categories

- Muc dich: Lay Get All Categories qua GET /api/categories.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: [GetAllCategoriesEndpoint.Parameters](./schemas.md#schema-getallcategoriesendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### POST /api/categories

- Muc dich: Thuc hien Create Category qua POST /api/categories.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Categories.Create
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateCategoryEndpoint.Request](./schemas.md#schema-createcategoryendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### GET /api/categories/{categoryId:guid}

- Muc dich: Lay Get Category By Id qua GET /api/categories/{categoryId:guid}.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - categoryId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found

### PUT /api/categories/{categoryId:guid}

- Muc dich: Cap nhat Update Category qua PUT /api/categories/{categoryId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Categories.Update
- Headers: Authorization: Bearer <token>
- Path params:
  - categoryId: Guid
- Query params: none
- Request body: [UpdateCategoryEndpoint.Request](./schemas.md#schema-updatecategoryendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request, 404 Not Found

### GET /api/categories/{categoryId:guid}/children

- Muc dich: Lay Get Category Children qua GET /api/categories/{categoryId:guid}/children.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - categoryId: Guid
- Query params: [GetCategoryChildrenEndpoint.Parameters](./schemas.md#schema-getcategorychildrenendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### GET /api/categories/by-slug/{slug}

- Muc dich: Lay Get Category By Slug qua GET /api/categories/by-slug/{slug}.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - slug: string
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found

### POST /api/items

- Muc dich: Thuc hien Create Item qua POST /api/items.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Create
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateItemEndpoint.Request](./schemas.md#schema-createitemendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### GET /api/items/{itemId:guid}

- Muc dich: Lay Get Item By Id qua GET /api/items/{itemId:guid}.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [ItemDto](./schemas.md#schema-itemdto)
- Error statuses: none documented

### POST /api/items/{itemId:guid}/activate

- Muc dich: Thuc hien Activate Item qua POST /api/items/{itemId:guid}/activate.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Activate
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
  - 400 Bad Request: none
  - 422 Unprocessable Entity: none
- Error statuses: 404 Not Found

### POST /api/items/{itemId:guid}/auctions

- Muc dich: Thuc hien Create Auction From Item qua POST /api/items/{itemId:guid}/auctions.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Auctions.Create
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [CreateAuctionFromItemEndpoint.Request](./schemas.md#schema-createauctionfromitemendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### POST /api/items/{itemId:guid}/confirm-inspected-condition

- Muc dich: Thuc hien Confirm Inspected Condition qua POST /api/items/{itemId:guid}/confirm-inspected-condition.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Resubmit
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [ItemDto](./schemas.md#schema-itemdto)
- Error statuses: 400 Bad Request, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/items/{itemId:guid}/media

- Muc dich: Thuc hien Add Item Image qua POST /api/items/{itemId:guid}/media.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.ManageMedia
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [AddItemImageEndpoint.Request](./schemas.md#schema-additemimageendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### DELETE /api/items/{itemId:guid}/media/{mediaId:guid}

- Muc dich: Xoa Remove Item Media qua DELETE /api/items/{itemId:guid}/media/{mediaId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.ManageMedia
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
  - mediaId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### POST /api/items/{itemId:guid}/media/{mediaId:guid}/primary

- Muc dich: Thuc hien Set Primary Image qua POST /api/items/{itemId:guid}/media/{mediaId:guid}/primary.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.ManageMedia
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
  - mediaId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### PUT /api/items/{itemId:guid}/media/reorder

- Muc dich: Cap nhat Reorder Item Media qua PUT /api/items/{itemId:guid}/media/reorder.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.ManageMedia
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [ReorderItemMediaEndpoint.Request](./schemas.md#schema-reorderitemmediaendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/items/{itemId:guid}/questions

- Muc dich: Lay Get Public Item Questions qua GET /api/items/{itemId:guid}/questions.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: [GetPublicItemQuestionsEndpoint.Parameters](./schemas.md#schema-getpublicitemquestionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 404 Not Found

### POST /api/items/{itemId:guid}/questions

- Muc dich: Thuc hien Ask Question qua POST /api/items/{itemId:guid}/questions.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.AskQuestion
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [AskQuestionEndpoint.Request](./schemas.md#schema-askquestionendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### POST /api/items/{itemId:guid}/questions/{questionId:guid}/answer

- Muc dich: Thuc hien Answer Question qua POST /api/items/{itemId:guid}/questions/{questionId:guid}/answer.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
  - questionId: Guid
- Query params: none
- Request body: [AnswerQuestionEndpoint.Request](./schemas.md#schema-answerquestionendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 404 Not Found

### POST /api/items/{itemId:guid}/resubmit

- Muc dich: Thuc hien Resubmit Item qua POST /api/items/{itemId:guid}/resubmit.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Resubmit
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [ResubmitItemEndpoint.Request](./schemas.md#schema-resubmititemendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/items/{itemId:guid}/shipping

- Muc dich: Thuc hien Choose Item Shipping qua POST /api/items/{itemId:guid}/shipping.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Create
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [ChooseItemShippingEndpoint.Request](./schemas.md#schema-chooseitemshippingendpoint-request)
- Success responses:
  - 201 Created: none
- Error statuses: 400 Bad Request

### POST /api/items/{itemId:guid}/submit

- Muc dich: Thuc hien Submit Item qua POST /api/items/{itemId:guid}/submit.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Items.Create
- Headers: Authorization: Bearer <token>
- Path params:
  - itemId: Guid
- Query params: none
- Request body: [SubmitItemEndpoint.Request](./schemas.md#schema-submititemendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/items/my

- Muc dich: Lay Get My Items qua GET /api/items/my.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyItemsEndpoint.Parameters](./schemas.md#schema-getmyitemsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented

### GET /api/sellers/{sellerId:guid}

- Muc dich: Lay Get Seller By Id qua GET /api/sellers/{sellerId:guid}.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - sellerId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [PublicSellerProfileDto](./schemas.md#schema-publicsellerprofiledto)
- Error statuses: none documented

### GET /api/sellers/{sellerId:guid}/items

- Muc dich: Lay Get Seller Items qua GET /api/sellers/{sellerId:guid}/items.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - sellerId: Guid
- Query params: [GetSellerItemsEndpoint.Parameters](./schemas.md#schema-getselleritemsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<PublicSellerItemDto>](./schemas.md#schema-pagedlist-publicselleritemdto)
- Error statuses: none documented


