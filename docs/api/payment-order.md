# Payment + Order

Bao gom Payments, Wallet, VNPay, Orders/Returns va admin payment ops.

### GET /api/admin/payments/escrows

- Muc dich: Lay Get Escrows qua GET /api/admin/payments/escrows.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetEscrowsEndpoint.Parameters](./schemas.md#schema-getescrowsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<EscrowDto>](./schemas.md#schema-pagedlist-escrowdto)
- Error statuses: 400 Bad Request, 403 Forbidden

### GET /api/admin/payments/escrows/{escrowId:guid}

- Muc dich: Lay Get Escrow By Id qua GET /api/admin/payments/escrows/{escrowId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params:
  - escrowId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [EscrowDetailDto](./schemas.md#schema-escrowdetaildto)
- Error statuses: 403 Forbidden, 404 Not Found

### GET /api/admin/payments/summary

- Muc dich: Lay Get Payment Summary qua GET /api/admin/payments/summary.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [PaymentSummaryDto](./schemas.md#schema-paymentsummarydto)
- Error statuses: 403 Forbidden

### GET /api/admin/payments/transactions

- Muc dich: Lay Get Transactions qua GET /api/admin/payments/transactions.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetTransactionsEndpoint.Parameters](./schemas.md#schema-gettransactionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<PaymentTransactionDto>](./schemas.md#schema-pagedlist-paymenttransactiondto)
- Error statuses: 400 Bad Request, 403 Forbidden

### GET /api/admin/payments/transactions/{transactionId:guid}

- Muc dich: Lay Get Transaction By Id qua GET /api/admin/payments/transactions/{transactionId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params:
  - transactionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [PaymentTransactionDto](./schemas.md#schema-paymenttransactiondto)
- Error statuses: 403 Forbidden, 404 Not Found

### GET /api/admin/payments/withdrawals

- Muc dich: Lay Get Withdrawals qua GET /api/admin/payments/withdrawals.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetWithdrawalsEndpoint.Parameters](./schemas.md#schema-getwithdrawalsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<WithdrawalRequestDto>](./schemas.md#schema-pagedlist-withdrawalrequestdto)
- Error statuses: 400 Bad Request, 403 Forbidden

### GET /api/admin/payments/withdrawals/{withdrawalId:guid}

- Muc dich: Lay Get Withdrawal By Id qua GET /api/admin/payments/withdrawals/{withdrawalId:guid}.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ReadPayments
- Headers: Authorization: Bearer <token>
- Path params:
  - withdrawalId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [AdminWithdrawalRequestDetailDto](./schemas.md#schema-adminwithdrawalrequestdetaildto)
- Error statuses: 403 Forbidden, 404 Not Found

### POST /api/admin/payments/withdrawals/{withdrawalId:guid}/approve

- Muc dich: Thuc hien Approve Withdrawal qua POST /api/admin/payments/withdrawals/{withdrawalId:guid}/approve.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManagePayments
- Headers: Authorization: Bearer <token>
- Path params:
  - withdrawalId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: 403 Forbidden, 404 Not Found

### POST /api/admin/payments/withdrawals/{withdrawalId:guid}/reject

- Muc dich: Thuc hien Reject Withdrawal qua POST /api/admin/payments/withdrawals/{withdrawalId:guid}/reject.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManagePayments
- Headers: Authorization: Bearer <token>
- Path params:
  - withdrawalId: Guid
- Query params: none
- Request body: [RejectWithdrawalEndpoint.Request](./schemas.md#schema-rejectwithdrawalendpoint-request)
- Success responses:
  - 204 No Content: none
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: 403 Forbidden, 404 Not Found

### GET /api/orders/{orderId:guid}

- Muc dich: Lay Get Order By Id qua GET /api/orders/{orderId:guid}.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [OrderDto](./schemas.md#schema-orderdto)
- Error statuses: 401 Unauthorized, 403 Forbidden, 404 Not Found

### POST /api/orders/{orderId:guid}/returns

- Muc dich: Thuc hien Create Order Return qua POST /api/orders/{orderId:guid}/returns.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
- Query params: none
- Request body: [CreateOrderReturnEndpoint.Request](./schemas.md#schema-createorderreturnendpoint-request)
- Success responses:
  - 200 OK: [OrderReturnDto](./schemas.md#schema-orderreturndto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/orders/{orderId:guid}/returns/{returnId:guid}/approve

- Muc dich: Thuc hien Approve Order Return qua POST /api/orders/{orderId:guid}/returns/{returnId:guid}/approve.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
  - returnId: Guid
- Query params: none
- Request body: [ApproveOrderReturnEndpoint.Request](./schemas.md#schema-approveorderreturnendpoint-request)
- Success responses:
  - 200 OK: [OrderReturnDto](./schemas.md#schema-orderreturndto)
- Error statuses: 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/orders/{orderId:guid}/returns/{returnId:guid}/confirm-received

- Muc dich: Thuc hien Confirm Order Return Received qua POST /api/orders/{orderId:guid}/returns/{returnId:guid}/confirm-received.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
  - returnId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [OrderReturnDto](./schemas.md#schema-orderreturndto)
- Error statuses: 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/orders/{orderId:guid}/returns/{returnId:guid}/reject

- Muc dich: Thuc hien Reject Order Return qua POST /api/orders/{orderId:guid}/returns/{returnId:guid}/reject.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
  - returnId: Guid
- Query params: none
- Request body: [RejectOrderReturnEndpoint.Request](./schemas.md#schema-rejectorderreturnendpoint-request)
- Success responses:
  - 200 OK: [OrderReturnDto](./schemas.md#schema-orderreturndto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/orders/{orderId:guid}/returns/{returnId:guid}/ship

- Muc dich: Thuc hien Ship Order Return qua POST /api/orders/{orderId:guid}/returns/{returnId:guid}/ship.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - orderId: Guid
  - returnId: Guid
- Query params: none
- Request body: [ShipOrderReturnEndpoint.Request](./schemas.md#schema-shiporderreturnendpoint-request)
- Success responses:
  - 200 OK: [OrderReturnDto](./schemas.md#schema-orderreturndto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/payments/checkout

- Muc dich: Thuc hien Checkout Order qua POST /api/payments/checkout.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CheckoutOrder.Request](./schemas.md#schema-checkoutorder-request)
- Success responses:
  - 201 Created: [CheckoutOrderResponse](./schemas.md#schema-checkoutorderresponse)
- Error statuses: none documented

### GET /api/payments/methods

- Muc dich: Lay Get Payment Methods qua GET /api/payments/methods.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<PaymentMethodDto>](./schemas.md#schema-ireadonlylist-paymentmethoddto)
- Error statuses: 401 Unauthorized

### POST /api/payments/methods

- Muc dich: Thuc hien Add Payment Method qua POST /api/payments/methods.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [AddPaymentMethodEndpoint.Request](./schemas.md#schema-addpaymentmethodendpoint-request)
- Success responses:
  - 200 OK: [Guid](./schemas.md#schema-guid)
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: none documented

### DELETE /api/payments/methods/{id:guid}

- Muc dich: Xoa Delete Payment Method qua DELETE /api/payments/methods/{id:guid}.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - id: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: none documented

### POST /api/payments/methods/{id:guid}/default

- Muc dich: Thuc hien Set Default Payment Method qua POST /api/payments/methods/{id:guid}/default.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - id: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: none documented

### POST /api/payments/vnpay/create-url

- Muc dich: Thuc hien Create Vn Pay Payment Url qua POST /api/payments/vnpay/create-url.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateVnPayPaymentUrlEndpoint.Request](./schemas.md#schema-createvnpaypaymenturlendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request
- Notes:
  - `Purpose`: `AuctionDeposit`, `OrderPayment`, `AuctionBuyNow`, `WalletTopUp`. Alias `Deposit` duoc map sang `AuctionDeposit`.
  - `Description`: co the gui unicode, backend se tu normalize sang ASCII hop le cho `vnp_OrderInfo`.
  - `BuyNowReservationId`: bat buoc khi `Purpose = AuctionBuyNow`.
  - `paymentUrl` tra ve se bao gom `vnp_ExpireDate` = `vnp_CreateDate` + 15 phut (GMT+7).

### GET /api/payments/vnpay/ipn

- Muc dich: Lay Vn Pay Ipn qua GET /api/payments/vnpay/ipn.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: none documented
- Notes:
  - IPN server-to-server tu VNPay.
  - Callback mapping uu tien transaction references co cau truc (`AuctionId`, `BuyNowReservationId`) thay vi parse description string.

### POST /api/payments/vnpay/refund

- Muc dich: Thuc hien Refund Vn Pay qua POST /api/payments/vnpay/refund.
- Audience: admin
- Auth: Bearer token + permission App.Permissions.Catalogs.Admin.ManagePayments
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [RefundVnPayEndpoint.Request](./schemas.md#schema-refundvnpayendpoint-request)
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request

### GET /api/payments/vnpay/return

- Muc dich: Lay Vn Pay Return qua GET /api/payments/vnpay/return.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: none
- Error statuses: 400 Bad Request
- Notes:
  - Return URL cho browser redirect sau thanh toan VNPay.


