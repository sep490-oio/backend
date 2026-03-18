# User + Auth

Bao gom Auth, Terms, Me, wallet user-facing, verification va seller profile.

### POST /api/auth/confirm-email

- Muc dich: Thuc hien Confirm Email qua POST /api/auth/confirm-email.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [ConfirmEmailEndpoint.Request](./schemas.md#schema-confirmemailendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/auth/forgot-password

- Muc dich: Thuc hien Forgot Password qua POST /api/auth/forgot-password.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [ForgotPasswordEndpoint.Request](./schemas.md#schema-forgotpasswordendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/auth/login

- Muc dich: Thuc hien Login User qua POST /api/auth/login.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [LoginUserEndpoint.Request](./schemas.md#schema-loginuserendpoint-request)
- Success responses:
  - 201 Created: [AuthTokenDto](./schemas.md#schema-authtokendto)
- Error statuses: none documented

### POST /api/auth/logout

- Muc dich: Thuc hien Logout User qua POST /api/auth/logout.
- Audience: authenticated
- Auth: Bearer token + permission App.Policy.ExpiredTokenAllowed
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [LogoutUserEndpoint.Request](./schemas.md#schema-logoutuserendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/auth/refresh

- Muc dich: Thuc hien Refresh Token qua POST /api/auth/refresh.
- Audience: authenticated
- Auth: Bearer token + permission App.Policy.ExpiredTokenAllowed
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [RefreshTokenEndpoint.Request](./schemas.md#schema-refreshtokenendpoint-request)
- Success responses:
  - 201 Created: [AuthTokenDto](./schemas.md#schema-authtokendto)
- Error statuses: none documented

### POST /api/auth/register

- Muc dich: Thuc hien Register User qua POST /api/auth/register.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [RegisterUserEndpoint.Request](./schemas.md#schema-registeruserendpoint-request)
- Success responses:
  - 201 Created: [UserDto](./schemas.md#schema-userdto)
- Error statuses: none documented

### POST /api/auth/resend-confirm-email

- Muc dich: Thuc hien Resend Confirm Email qua POST /api/auth/resend-confirm-email.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [ResendConfirmEmailEndpoint.Request](./schemas.md#schema-resendconfirmemailendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### POST /api/auth/reset-password

- Muc dich: Thuc hien Reset Password qua POST /api/auth/reset-password.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: [ResetPasswordEndpoint.Request](./schemas.md#schema-resetpasswordendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: 400 Bad Request

### GET /api/me/addresses

- Muc dich: Lay Get Addresses qua GET /api/me/addresses.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadAddress
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetAddressesEndpoint.Parameters](./schemas.md#schema-getaddressesendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<UserAddressDto>](./schemas.md#schema-pagedlist-useraddressdto)
- Error statuses: none documented

### POST /api/me/addresses

- Muc dich: Thuc hien Add Address qua POST /api/me/addresses.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageAddress
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [AddAddressEndpoint.Request](./schemas.md#schema-addaddressendpoint-request)
- Success responses:
  - 201 Created: [UserAddressDto](./schemas.md#schema-useraddressdto)
- Error statuses: none documented

### DELETE /api/me/addresses/{addressId:guid}

- Muc dich: Xoa Remove Address qua DELETE /api/me/addresses/{addressId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageAddress
- Headers: Authorization: Bearer <token>
- Path params:
  - addressId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### PUT /api/me/addresses/{addressId:guid}

- Muc dich: Cap nhat Update Address qua PUT /api/me/addresses/{addressId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageAddress
- Headers: Authorization: Bearer <token>
- Path params:
  - addressId: Guid
- Query params: none
- Request body: [UpdateAddressEndpoint.Request](./schemas.md#schema-updateaddressendpoint-request)
- Success responses:
  - 200 OK: [UserAddressDto](./schemas.md#schema-useraddressdto)
- Error statuses: none documented

### PATCH /api/me/addresses/{addressId:guid}/default

- Muc dich: Cap nhat Set Default Address qua PATCH /api/me/addresses/{addressId:guid}/default.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageAddress
- Headers: Authorization: Bearer <token>
- Path params:
  - addressId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/me/auctions

- Muc dich: Lay Get My Auctions qua GET /api/me/auctions.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadAuctions
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyAuctionsEndpoint.Parameters](./schemas.md#schema-getmyauctionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<AuctionListItemDto>](./schemas.md#schema-pagedlist-auctionlistitemdto)
- Error statuses: none documented

### GET /api/me/auctions/watch-list

- Muc dich: Lay Get My Watchlist qua GET /api/me/auctions/watch-list.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadWatchlist
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyWatchlistEndpoint.Parameters](./schemas.md#schema-getmywatchlistendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<MyAuctionWatchlistDto>](./schemas.md#schema-pagedlist-myauctionwatchlistdto)
- Error statuses: none documented

### GET /api/me/bids

- Muc dich: Lay Get My Bids qua GET /api/me/bids.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadBids
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyBidsEndpoint.Parameters](./schemas.md#schema-getmybidsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<MyBidDto>](./schemas.md#schema-pagedlist-mybiddto)
- Error statuses: none documented

### GET /api/me/login-history

- Muc dich: Lay Get Login History qua GET /api/me/login-history.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadLoginHistory
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetLoginHistoryEndpoint.Parameters](./schemas.md#schema-getloginhistoryendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<LoginHistoryDto>](./schemas.md#schema-pagedlist-loginhistorydto)
- Error statuses: none documented

### GET /api/me/orders

- Muc dich: Lay Get My Orders qua GET /api/me/orders.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<OrderDto>](./schemas.md#schema-ireadonlylist-orderdto)
- Error statuses: 401 Unauthorized

### PUT /api/me/password

- Muc dich: Cap nhat Change Password qua PUT /api/me/password.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ChangePassword
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [ChangePasswordEndpoint.Request](./schemas.md#schema-changepasswordendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### PUT /api/me/phone

- Muc dich: Cap nhat Set Phone Number qua PUT /api/me/phone.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManagePhone
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [SetPhoneNumberEndpoint.Request](./schemas.md#schema-setphonenumberendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/me/phone/confirm

- Muc dich: Thuc hien Confirm Phone Number qua POST /api/me/phone/confirm.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManagePhone
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [ConfirmPhoneNumberEndpoint.Request](./schemas.md#schema-confirmphonenumberendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/me/profile

- Muc dich: Lay Get Current User Profile qua GET /api/me/profile.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadProfile
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [UserProfileDto](./schemas.md#schema-userprofiledto)
- Error statuses: none documented

### PUT /api/me/profile

- Muc dich: Cap nhat Update Current User Profile qua PUT /api/me/profile.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.UpdateProfile
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [UpdateCurrentUserProfileEndpoint.Request](./schemas.md#schema-updatecurrentuserprofileendpoint-request)
- Success responses:
  - 200 OK: [UserProfileDto](./schemas.md#schema-userprofiledto)
- Error statuses: none documented

### GET /api/me/seller-profile

- Muc dich: Lay Get My Seller Profile qua GET /api/me/seller-profile.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadSellerProfile
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [SellerProfileDto](./schemas.md#schema-sellerprofiledto)
- Error statuses: none documented

### POST /api/me/seller-profile

- Muc dich: Thuc hien Create Seller Profile qua POST /api/me/seller-profile.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageSellerProfile
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateSellerProfileEndpoint.Request](./schemas.md#schema-createsellerprofileendpoint-request)
- Success responses:
  - 201 Created: [SellerProfileDto](./schemas.md#schema-sellerprofiledto)
- Error statuses: none documented

### PUT /api/me/seller-profile

- Muc dich: Cap nhat Update Seller Profile qua PUT /api/me/seller-profile.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageSellerProfile
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [UpdateSellerProfileEndpoint.Request](./schemas.md#schema-updatesellerprofileendpoint-request)
- Success responses:
  - 200 OK: [SellerProfileDto](./schemas.md#schema-sellerprofiledto)
- Error statuses: none documented

### GET /api/me/sessions

- Muc dich: Lay Get Active Sessions qua GET /api/me/sessions.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadSessions
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetActiveSessionsEndpoint.Parameters](./schemas.md#schema-getactivesessionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<UserSessionDto>](./schemas.md#schema-pagedlist-usersessiondto)
- Error statuses: none documented

### GET /api/me/terms

- Muc dich: Lay Get My Accepted Terms qua GET /api/me/terms.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadTerms
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<TermsAcceptanceDto>](./schemas.md#schema-ireadonlylist-termsacceptancedto)
- Error statuses: none documented

### POST /api/me/terms/{termDocumentId:guid}/accept

- Muc dich: Thuc hien Accept Term qua POST /api/me/terms/{termDocumentId:guid}/accept.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.AcceptTerms
- Headers: Authorization: Bearer <token>
- Path params:
  - termDocumentId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 201 Created: [TermsAcceptanceDto](./schemas.md#schema-termsacceptancedto)
- Error statuses: 404 Not Found, 409 Conflict

### POST /api/me/two-factor/disable

- Muc dich: Thuc hien Disable Two Factor qua POST /api/me/two-factor/disable.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageTwoFactor
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/me/two-factor/enable

- Muc dich: Thuc hien Enable Two Factor qua POST /api/me/two-factor/enable.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageTwoFactor
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [EnableTwoFactorEndpoint.Request](./schemas.md#schema-enabletwofactorendpoint-request)
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/me/verifications

- Muc dich: Lay Get My Verifications qua GET /api/me/verifications.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadVerification
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyCollection<VerificationSummaryDto>](./schemas.md#schema-ireadonlycollection-verificationsummarydto)
- Error statuses: none documented

### POST /api/me/verifications

- Muc dich: Thuc hien Create Verification qua POST /api/me/verifications.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateVerificationEndpoint.Request](./schemas.md#schema-createverificationendpoint-request)
- Success responses:
  - 201 Created: [VerificationDto](./schemas.md#schema-verificationdto)
- Error statuses: none documented

### GET /api/me/verifications/{verificationId:guid}

- Muc dich: Lay Get My Verification By Id qua GET /api/me/verifications/{verificationId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ReadVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [VerificationDto](./schemas.md#schema-verificationdto)
- Error statuses: none documented

### PUT /api/me/verifications/{verificationId:guid}

- Muc dich: Cap nhat Update Verification qua PUT /api/me/verifications/{verificationId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: [UpdateVerificationEndpoint.Request](./schemas.md#schema-updateverificationendpoint-request)
- Success responses:
  - 200 OK: [VerificationDto](./schemas.md#schema-verificationdto)
- Error statuses: none documented

### POST /api/me/verifications/{verificationId:guid}/disputes

- Muc dich: Thuc hien Create Verification Dispute qua POST /api/me/verifications/{verificationId:guid}/disputes.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: [CreateVerificationDisputeEndpoint.Request](./schemas.md#schema-createverificationdisputeendpoint-request)
- Success responses:
  - 201 Created: [DisputeThreadMetaDto](./schemas.md#schema-disputethreadmetadto)
- Error statuses: 400 Bad Request, 401 Unauthorized, 403 Forbidden, 404 Not Found, 409 Conflict

### POST /api/me/verifications/{verificationId:guid}/documents

- Muc dich: Thuc hien Upload Verification Document qua POST /api/me/verifications/{verificationId:guid}/documents.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: [UploadVerificationDocumentEndpoint.Request](./schemas.md#schema-uploadverificationdocumentendpoint-request)
- Success responses:
  - 201 Created: [VerificationDocumentDto](./schemas.md#schema-verificationdocumentdto)
- Error statuses: none documented

### DELETE /api/me/verifications/{verificationId:guid}/documents/{docId:guid}

- Muc dich: Xoa Delete Verification Document qua DELETE /api/me/verifications/{verificationId:guid}/documents/{docId:guid}.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
  - docId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### POST /api/me/verifications/{verificationId:guid}/submit

- Muc dich: Thuc hien Submit Verification qua POST /api/me/verifications/{verificationId:guid}/submit.
- Audience: authenticated
- Auth: Bearer token + permission App.Permissions.Catalogs.Me.ManageVerification
- Headers: Authorization: Bearer <token>
- Path params:
  - verificationId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 204 No Content: none
- Error statuses: none documented

### GET /api/me/wallet

- Muc dich: Lay Get My Wallet qua GET /api/me/wallet.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [WalletSummaryDto](./schemas.md#schema-walletsummarydto)
- Error statuses: 401 Unauthorized, 404 Not Found

### GET /api/me/wallet/transactions

- Muc dich: Lay Get My Wallet Transactions qua GET /api/me/wallet/transactions.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyWalletTransactionsEndpoint.Parameters](./schemas.md#schema-getmywallettransactionsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<WalletTransactionDto>](./schemas.md#schema-pagedlist-wallettransactiondto)
- Error statuses: 400 Bad Request, 401 Unauthorized

### GET /api/me/wallet/transactions/{transactionId:guid}

- Muc dich: Lay Get My Wallet Transaction By Id qua GET /api/me/wallet/transactions/{transactionId:guid}.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - transactionId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [WalletTransactionDto](./schemas.md#schema-wallettransactiondto)
- Error statuses: 401 Unauthorized, 404 Not Found

### GET /api/me/wallet/withdrawals

- Muc dich: Lay Get My Withdrawals qua GET /api/me/wallet/withdrawals.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: [GetMyWithdrawalsEndpoint.Parameters](./schemas.md#schema-getmywithdrawalsendpoint-parameters)
- Request body: none
- Success responses:
  - 200 OK: [PagedList<WithdrawalRequestDto>](./schemas.md#schema-pagedlist-withdrawalrequestdto)
- Error statuses: 400 Bad Request, 401 Unauthorized

### POST /api/me/wallet/withdrawals

- Muc dich: Thuc hien Create Withdrawal qua POST /api/me/wallet/withdrawals.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params: none
- Query params: none
- Request body: [CreateWithdrawalEndpoint.Request](./schemas.md#schema-createwithdrawalendpoint-request)
- Success responses:
  - 200 OK: [CreateWithdrawalRequestResponse](./schemas.md#schema-createwithdrawalrequestresponse)
  - 400 Bad Request: [Error](./schemas.md#schema-error)
- Error statuses: 401 Unauthorized, 404 Not Found

### POST /api/me/wallet/withdrawals/{withdrawalId:guid}/cancel

- Muc dich: Thuc hien Cancel Withdrawal qua POST /api/me/wallet/withdrawals/{withdrawalId:guid}/cancel.
- Audience: authenticated
- Auth: Bearer token
- Headers: Authorization: Bearer <token>
- Path params:
  - withdrawalId: Guid
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [WithdrawalRequestDto](./schemas.md#schema-withdrawalrequestdto)
- Error statuses: 401 Unauthorized, 404 Not Found, 409 Conflict

### GET /api/terms/{type}/active

- Muc dich: Lay Get Active Terms By Type qua GET /api/terms/{type}/active.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params:
  - type: string
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [TermsDocumentDto](./schemas.md#schema-termsdocumentdto)
- Error statuses: 404 Not Found

### GET /api/terms/active

- Muc dich: Lay Get Active Terms qua GET /api/terms/active.
- Audience: anonymous
- Auth: AllowAnonymous
- Headers: Khong co header dac biet
- Path params: none
- Query params: none
- Request body: none
- Success responses:
  - 200 OK: [IReadOnlyList<TermsDocumentDto>](./schemas.md#schema-ireadonlylist-termsdocumentdto)
- Error statuses: none documented


