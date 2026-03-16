# Schemas appendix

File nay gom request/response/DTO duoc tham chieu tu HTTP API va SignalR docs.
<a id="schema-acknowledgemonitoringalertendpoint-request"></a>
## AcknowledgeMonitoringAlertEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\AcknowledgeMonitoringAlertEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Notes` | string | optional |

<a id="schema-addaddressendpoint-request"></a>
## AddAddressEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\AddAddressEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Type` | string | required |
| `RecipientName` | string | required |
| `Street` | string | required |
| `Ward` | string | required |
| `District` | string | required |
| `City` | string | required |
| `PostalCode` | string | optional |
| `PhoneNumber` | string | required |
| `CountryCode` | string | required |
| `IsDefault` | boolean | required |

<a id="schema-additemimageendpoint-request"></a>
## AddItemImageEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\AddItemImageEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MediaUploadId` | guid | optional |
| `IsPrimary` | boolean | default = false |
| `SortOrder` | number | default = null |

<a id="schema-addpaymentmethodendpoint-request"></a>
## AddPaymentMethodEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\PaymentMethods\AddPaymentMethodEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Type` | string | optional |
| `Provider` | string | optional |
| `LastFour` | string | optional |
| `ExpiryMonth` | number | optional |
| `ExpiryYear` | number | optional |
| `HolderName` | string | optional |
| `TokenReference` | string | optional |
| `IsDefault` | boolean | required |

<a id="schema-adminescrowfilterparameters"></a>
## AdminEscrowFilterParameters

- Source: .\src\core\OIO.Application\Context\PaymentContext\Queries\Admins\GetAdminEscrows\GetAdminEscrowsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `OrderId` | guid | optional |
| `BuyerId` | guid | optional |
| `SellerId` | guid | optional |

<a id="schema-admintransactionfilterparameters"></a>
## AdminTransactionFilterParameters

- Source: .\src\core\OIO.Application\Context\PaymentContext\Queries\Admins\GetAdminTransactions\GetAdminTransactionsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `Type` | string | optional |
| `UserId` | guid | optional |
| `OrderId` | guid | optional |

<a id="schema-adminwithdrawalfilterparameters"></a>
## AdminWithdrawalFilterParameters

- Source: .\src\core\OIO.Application\Context\PaymentContext\Queries\Admins\GetAdminWithdrawals\GetAdminWithdrawalsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `UserId` | guid | optional |

<a id="schema-adminwithdrawalrequestdetaildto"></a>
## AdminWithdrawalRequestDetailDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\AdminWithdrawalRequestDetailDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `UserId` | guid | optional |
| `WalletId` | guid | optional |
| `Amount` | number | required |
| `Fee` | number | required |
| `NetAmount` | number | required |
| `Status` | string | optional |
| `BankName` | string | optional |
| `AccountNumber` | string | optional |
| `AccountHolder` | string | optional |
| `RejectionReason` | string | optional |
| `ProcessedBy` | guid | optional |
| `CreatedAt` | datetime | optional |
| `ProcessedAt` | datetime | optional |

<a id="schema-answerquestionendpoint-request"></a>
## AnswerQuestionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\AnswerQuestionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Answer` | string | optional |

<a id="schema-approveorderreturnendpoint-request"></a>
## ApproveOrderReturnEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\OrderContext\ApproveOrderReturnEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Notes` | string | optional |

<a id="schema-askquestionendpoint-request"></a>
## AskQuestionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\AskQuestionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Question` | string | optional |

<a id="schema-assignitemreviewerendpoint-request"></a>
## AssignItemReviewerEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\AssignItemReviewerEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AdminId` | guid | optional |

<a id="schema-assignreportendpoint-request"></a>
## AssignReportEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\AssignReportEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AssignedToUserId` | guid | optional |

<a id="schema-auctioncancellednotification"></a>
## AuctionCancelledNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `Reason` | string | optional |

<a id="schema-auctiondto"></a>
## AuctionDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\AuctionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ItemId` | guid | optional |
| `SellerId` | guid | optional |
| `AuctionType` | string | optional |
| `StartingPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `ReservePrice` | [MoneyDto?](./schemas.md#schema-moneydto) | optional |
| `BuyNowPrice` | [MoneyDto?](./schemas.md#schema-moneydto) | optional |
| `CurrentPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `BidIncrement` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `Currency` | string | optional |
| `StartTime` | datetime | optional |
| `EndTime` | datetime | optional |
| `ActualEndTime` | datetime | optional |
| `QualificationStartAt` | datetime | optional |
| `QualificationEndAt` | datetime | optional |
| `Status` | string | optional |
| `CurrentWinnerId` | guid | optional |
| `AutoExtend` | boolean | required |
| `ExtensionMinutes` | number | required |
| `ExtensionCount` | number | required |
| `AssignedAdminId` | guid | optional |
| `AssignedAt` | datetime | optional |
| `IsFeatured` | boolean | required |
| `Priority` | number | required |
| `PriorityReason` | string | optional |
| `VerifyByPlatform` | boolean | required |
| `RejectionCount` | number | required |
| `ViewCount` | number | required |
| `BidCount` | number | required |
| `WatchCount` | number | required |
| `MinimumBidAmount` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `IsReserveMet` | boolean | required |
| `HasBuyNow` | boolean | required |
| `IsBuyNowReserved` | boolean | required |
| `BuyNowReservedUntil` | datetime | optional |
| `RemainingTime` | duration | optional |
| `IsEndingSoon` | boolean | required |
| `CreatedAt` | datetime | optional |

<a id="schema-auctionendednotification"></a>
## AuctionEndedNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `WinnerId` | guid | optional |
| `WinnerDisplayName` | string | optional |
| `FinalPrice` | number | required |
| `TotalBids` | number | required |
| `ReserveMet` | boolean | required |

<a id="schema-auctionextendednotification"></a>
## AuctionExtendedNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `NewEndTime` | datetime-offset | optional |
| `ExtensionMinutes` | number | required |

<a id="schema-auctionlistitemdto"></a>
## AuctionListItemDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\AuctionListItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ItemTitle` | string | optional |
| `PrimaryImageUrl` | string | optional |
| `CurrentPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `StartingPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `BuyNowPrice` | [MoneyDto?](./schemas.md#schema-moneydto) | optional |
| `IsBuyNowReserved` | boolean | required |
| `BuyNowReservedUntil` | datetime | optional |
| `Currency` | string | optional |
| `Status` | string | optional |
| `BidCount` | number | required |
| `WatchCount` | number | required |
| `StartTime` | datetime | optional |
| `EndTime` | datetime | optional |
| `RemainingTime` | duration | optional |
| `IsEndingSoon` | boolean | optional |
| `IsFeatured` | boolean | optional |
| `SellerId` | guid | optional |

<a id="schema-auctionstartednotification"></a>
## AuctionStartedNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `StartTime` | datetime-offset | optional |
| `EndTime` | datetime-offset | optional |

<a id="schema-authtokendto"></a>
## AuthTokenDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\AuthTokenDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AccessToken` | string | optional |
| `RefreshToken` | string | optional |
| `AccessTokenExpiresAt` | datetime | optional |
| `RefreshTokenExpiresAt` | datetime | optional |
| `Session` | [SessionExpirationDto](./schemas.md#schema-sessionexpirationdto) | required |

<a id="schema-biddto"></a>
## BidDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\BidDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `AuctionId` | guid | optional |
| `BidderId` | guid | optional |
| `Amount` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `IsAutoBid` | boolean | required |
| `Status` | string | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-bidnotification"></a>
## BidNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `BidId` | guid | optional |
| `BidderId` | guid | optional |
| `BidderDisplayName` | string | optional |
| `Amount` | number | required |
| `CurrentPrice` | number | required |
| `MinimumNextBid` | number | required |
| `TotalBids` | number | required |
| `IsAutoBid` | boolean | required |
| `Timestamp` | datetime-offset | optional |

<a id="schema-bool"></a>
## bool

- Kieu du lieu co ban: boolean

<a id="schema-buynowcheckoutdto"></a>
## BuyNowCheckoutDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\BuyNowCheckoutDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ReservationId` | guid | optional |
| `PaymentUrl` | string | optional |
| `ExpiresAt` | datetime | optional |
| `BuyNowPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `DepositAppliedAmount` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `AmountDue` | [MoneyDto](./schemas.md#schema-moneydto) | required |

<a id="schema-buynownotification"></a>
## BuyNowNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `BuyerId` | guid | optional |
| `Price` | number | required |

<a id="schema-buynowreservationreleasednotification"></a>
## BuyNowReservationReleasedNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `ReservationId` | guid | optional |
| `BuyerId` | guid | optional |
| `Reason` | string | optional |
| `ReleasedAt` | datetime-offset | optional |

<a id="schema-buynowreservednotification"></a>
## BuyNowReservedNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `ReservationId` | guid | optional |
| `BuyerId` | guid | optional |
| `BuyNowPrice` | number | required |
| `DepositAppliedAmount` | number | required |
| `AmountDue` | number | required |
| `ExpiresAt` | datetime-offset | optional |

<a id="schema-cancelauctionendpoint-request"></a>
## CancelAuctionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\CancelAuctionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-cancelinboundshipmentendpoint-request"></a>
## CancelInboundShipmentEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\CancelInboundShipmentEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-cancelinvalidbidendpoint-request"></a>
## CancelInvalidBidEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\CancelInvalidBidEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-changepasswordendpoint-request"></a>
## ChangePasswordEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\ChangePasswordEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `CurrentPassword` | string | required |
| `NewPassword` | string | required |

<a id="schema-changeuserstatusendpoint-request"></a>
## ChangeUserStatusEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\ChangeUserStatusEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Status` | string | optional |

<a id="schema-checkoutorder-request"></a>
## CheckoutOrder.Request

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\CheckoutOrder.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `OrderId` | guid | optional |
| `BankCode` | string | optional |
| `ReturnUrl` | string | optional |

<a id="schema-checkoutorderresponse"></a>
## CheckoutOrderResponse

- Source: .\src\core\OIO.Application\Context\PaymentContext\Commands\CheckoutOrder\CheckoutOrderCommand.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `TransactionId` | guid | optional |
| `TransactionRef` | string | optional |
| `PaymentUrl` | string | optional |

<a id="schema-chooseauctionshippingendpoint-request"></a>
## ChooseAuctionShippingEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\ChooseAuctionShippingEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `SenderName` | string | optional |
| `SenderPhone` | string | optional |
| `SenderAddress` | string | optional |
| `SenderWard` | string | optional |
| `SenderDistrict` | string | optional |
| `SenderProvince` | string | optional |
| `WeightGrams` | number | required |
| `InsuranceValue` | number | required |
| `ProviderCode` | string | default = null |
| `SenderCarrierAddressDataJson` | string | default = null |
| `LengthCm` | number | default = null |
| `WidthCm` | number | default = null |
| `HeightCm` | number | default = null |
| `ExternalTrackingNumber` | string | default = null |
| `ExternalCarrierName` | string | default = null |
| `Notes` | string | default = null |

<a id="schema-chooseitemshippingendpoint-request"></a>
## ChooseItemShippingEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\ChooseItemShippingEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `SenderName` | string | optional |
| `SenderPhone` | string | optional |
| `SenderAddress` | string | optional |
| `SenderWard` | string | optional |
| `SenderDistrict` | string | optional |
| `SenderProvince` | string | optional |
| `WeightGrams` | number | required |
| `InsuranceValue` | number | required |
| `ProviderCode` | string | default = null |
| `SenderCarrierAddressDataJson` | string | default = null |
| `LengthCm` | number | default = null |
| `WidthCm` | number | default = null |
| `HeightCm` | number | default = null |
| `ExternalTrackingNumber` | string | default = null |
| `ExternalCarrierName` | string | default = null |
| `Notes` | string | default = null |

<a id="schema-configureautobidendpoint-request"></a>
## ConfigureAutoBidEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\ConfigureAutoBidEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MaxAmount` | number | required |
| `Currency` | string | default = "VND" |
| `IncrementAmount` | number | default = null |

<a id="schema-confirmemailendpoint-request"></a>
## ConfirmEmailEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\ConfirmEmailEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `UserId` | guid | required |
| `Token` | string | required |

<a id="schema-confirmphonenumberendpoint-request"></a>
## ConfirmPhoneNumberEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\ConfirmPhoneNumberEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `VerificationCode` | string | required |

<a id="schema-confirmuploadendpoint-request"></a>
## ConfirmUploadEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\MediaContext\Media\ConfirmUploadEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MediaUploadId` | guid | optional |
| `PublicId` | string | optional |
| `SecureUrl` | string | optional |
| `Bytes` | number | required |
| `Format` | string | optional |
| `FileName` | string | optional |
| `Width` | number | optional |
| `Height` | number | optional |
| `DurationSeconds` | number | optional |

<a id="schema-correctedinforequest"></a>
## CorrectedInfoRequest

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\CreateVerificationDisputeEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FullName` | string | required |
| `DateOfBirth` | date | required |
| `Gender` | string | required |
| `IdType` | string | required |
| `IdNumber` | string | required |
| `IdIssuedDate` | date | optional |
| `IdExpiredDate` | date | optional |
| `IdIssuedPlace` | string | optional |
| `FullAddress` | string | required |
| `Province` | string | required |
| `District` | string | required |
| `Ward` | string | required |
| `Nationality` | string | default = null |

<a id="schema-createauctionendpoint-request"></a>
## CreateAuctionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\CreateAuctionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Condition` | string | optional |
| `CategoryId` | guid | default = null |
| `Description` | string | default = null |
| `Quantity` | number | default = 1 |
| `Attributes` | string | default = null |
| `Media` | [MediaAttachment](./schemas.md#schema-mediaattachment)[] | default = null |
| `BidIncrement` | number | default = 0 |
| `ReservePrice` | number | default = null |
| `BuyNowPrice` | number | default = null |
| `ExtensionMinutes` | number | default = 5 |
| `Currency` | string | default = "VND" |
| `AuctionType` | string | default = "regular" |

<a id="schema-createauctionfromitemendpoint-request"></a>
## CreateAuctionFromItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\CreateAuctionFromItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StartingPrice` | number | default = 0 |
| `BidIncrement` | number | default = 0 |
| `ReservePrice` | number | default = null |
| `BuyNowPrice` | number | default = null |
| `ExtensionMinutes` | number | default = 5 |
| `Currency` | string | default = "VND" |
| `AuctionType` | string | default = "regular" |

<a id="schema-createcategoryendpoint-request"></a>
## CreateCategoryEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Categories\CreateCategoryEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Name` | string | optional |
| `Slug` | string | optional |
| `ParentId` | guid | default = null |
| `Description` | string | default = null |
| `MediaUploadId` | guid | default = null |
| `SortOrder` | number | default = 0 |

<a id="schema-createitemendpoint-request"></a>
## CreateItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\CreateItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Title` | string | optional |
| `Condition` | string | optional |
| `CategoryId` | guid | default = null |
| `Description` | string | default = null |
| `Quantity` | number | default = 1 |
| `Attributes` | string | default = null |
| `Images` | [MediaAttachmentRequest](./schemas.md#schema-mediaattachmentrequest)[] | default = null |

<a id="schema-createorderreturnendpoint-request"></a>
## CreateOrderReturnEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\OrderContext\CreateOrderReturnEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ReasonCode` | string | optional |
| `Description` | string | optional |

<a id="schema-createreportendpoint-request"></a>
## CreateReportEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Reports\CreateReportEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `EntityType` | string | optional |
| `EntityId` | guid | optional |
| `ReasonCode` | string | optional |
| `Description` | string | optional |
| `Attachments` | string | optional |

<a id="schema-createsellerprofileendpoint-request"></a>
## CreateSellerProfileEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\CreateSellerProfileEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StoreName` | string | required |
| `StoreDescription` | string | required |

<a id="schema-createtermsendpoint-request"></a>
## CreateTermsEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\CreateTermsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Type` | string | required |
| `MediaUploadId` | guid | required |

<a id="schema-createverificationdisputeendpoint-request"></a>
## CreateVerificationDisputeEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\CreateVerificationDisputeEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | required |
| `CorrectedInfo` | [CorrectedInfoRequest](./schemas.md#schema-correctedinforequest) | required |
| `Message` | string | default = null |
| `MediaUploadIds` | [Guid](./schemas.md#schema-guid)[] | default = null |

<a id="schema-createverificationendpoint-request"></a>
## CreateVerificationEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\CreateVerificationEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `VerificationType` | string | required |

<a id="schema-createvnpaypaymenturlendpoint-request"></a>
## CreateVnPayPaymentUrlEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\VnPay\CreateVnPayPaymentUrlEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |
| `Currency` | string | optional |
| `Purpose` | string | optional |
| `Description` | string | optional |
| `BankCode` | string | default = null |
| `AuctionId` | guid | default = null |
| `OrderId` | guid | default = null |

<a id="schema-createwithdrawalendpoint-request"></a>
## CreateWithdrawalEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\CreateWithdrawalEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |
| `BankName` | string | optional |
| `AccountNumber` | string | optional |
| `AccountHolder` | string | optional |

<a id="schema-createwithdrawalrequestresponse"></a>
## CreateWithdrawalRequestResponse

- Source: .\src\core\OIO.Application\Context\PaymentContext\Commands\Withdrawals\CreateWithdrawalRequestCommand.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `WithdrawalRequestId` | guid | optional |
| `Amount` | number | required |
| `Fee` | number | required |
| `NetAmount` | number | required |
| `Status` | string | optional |

<a id="schema-decimal"></a>
## decimal

- Kieu du lieu co ban: number

<a id="schema-decimal"></a>
## decimal?

- Kieu du lieu co ban: number

<a id="schema-dictionary-string-string"></a>
## Dictionary<string, string[]>?

- Kieu map string -> string[] dung cho validation errors hoac metadata tuong tu.

<a id="schema-disputemessageattachmentdto"></a>
## DisputeMessageAttachmentDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `FileName` | string | optional |
| `ResourceType` | string | optional |
| `SecureUrl` | string | optional |
| `Bytes` | number | required |
| `Format` | string | optional |
| `Width` | number | optional |
| `Height` | number | optional |
| `DurationSeconds` | number | optional |

<a id="schema-disputemessagedto"></a>
## DisputeMessageDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `DisputeId` | guid | optional |
| `SenderId` | guid | optional |
| `SenderDisplayName` | string | optional |
| `Message` | string | optional |
| `IsInternal` | boolean | required |
| `CreatedAt` | datetime | optional |
| `Attachments` | [DisputeMessageAttachmentDto](./schemas.md#schema-disputemessageattachmentdto)[] | required |

<a id="schema-disputemessagepagedto"></a>
## DisputeMessagePageDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Messages` | [DisputeMessageDto](./schemas.md#schema-disputemessagedto)[] | required |
| `HasMore` | boolean | required |
| `NextBeforeCreatedAt` | datetime | optional |
| `NextBeforeId` | guid | optional |

<a id="schema-disputeparticipantdto"></a>
## DisputeParticipantDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `UserId` | guid | optional |
| `DisplayName` | string | optional |
| `Role` | string | optional |
| `LastReadAt` | datetime | optional |

<a id="schema-disputeparticipantreadstatedto"></a>
## DisputeParticipantReadStateDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `DisputeId` | guid | optional |
| `UserId` | guid | optional |
| `LastReadMessageId` | guid | optional |
| `LastReadAt` | datetime | optional |

<a id="schema-disputeparticipantreadstatedto"></a>
## DisputeParticipantReadStateDto?

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `DisputeId` | guid | optional |
| `UserId` | guid | optional |
| `LastReadMessageId` | guid | optional |
| `LastReadAt` | datetime | optional |

<a id="schema-disputesummarydto"></a>
## DisputeSummaryDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `DisputeNumber` | string | optional |
| `Title` | string | optional |
| `Status` | string | optional |
| `Priority` | string | optional |
| `AuctionId` | guid | optional |
| `VerificationId` | guid | optional |
| `OrderId` | guid | optional |
| `LastMessagePreview` | string | optional |
| `LastMessageAt` | datetime | optional |
| `UnreadCount` | number | required |
| `AssignedTo` | guid | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-disputethreaddto"></a>
## DisputeThreadDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Meta` | [DisputeThreadMetaDto](./schemas.md#schema-disputethreadmetadto) | required |
| `Participants` | [DisputeParticipantDto](./schemas.md#schema-disputeparticipantdto)[] | required |
| `CurrentUserReadState` | [DisputeParticipantReadStateDto?](./schemas.md#schema-disputeparticipantreadstatedto) | optional |
| `RecentMessages` | [DisputeMessageDto](./schemas.md#schema-disputemessagedto)[] | required |

<a id="schema-disputethreadmetadto"></a>
## DisputeThreadMetaDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `DisputeNumber` | string | optional |
| `Title` | string | optional |
| `Status` | string | optional |
| `Priority` | string | optional |
| `AuctionId` | guid | optional |
| `VerificationId` | guid | optional |
| `OrderId` | guid | optional |
| `ComplainantId` | guid | optional |
| `RespondentId` | guid | optional |
| `AssignedTo` | guid | optional |
| `CreatedAt` | datetime | optional |
| `ResolvedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |

<a id="schema-disputeunreadupdatedto"></a>
## DisputeUnreadUpdateDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\DisputeDtos.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `DisputeId` | guid | optional |
| `UnreadCount` | number | required |

<a id="schema-enabletwofactorendpoint-request"></a>
## EnableTwoFactorEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\EnableTwoFactorEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Provider` | string | optional |

<a id="schema-error"></a>
## Error

- Source: .\src\core\OIO.Domain\SeedWork\Errors\Error.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Code` | string | optional |
| `Message` | string | optional |
| `Kind` | string | optional |

<a id="schema-errornotification"></a>
## ErrorNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Code` | string | optional |
| `Message` | string | optional |
| `Errors` | [Dictionary<string, string[]>?](./schemas.md#schema-dictionary-string-string) | optional |

<a id="schema-errornotification"></a>
## ErrorNotification?

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Code` | string | optional |
| `Message` | string | optional |
| `Errors` | [Dictionary<string, string[]>?](./schemas.md#schema-dictionary-string-string) | optional |

<a id="schema-escalatereportemergencyendpoint-request"></a>
## EscalateReportEmergencyEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\EscalateReportEmergencyEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ReasonOverride` | string | optional |

<a id="schema-escrowdetaildto"></a>
## EscrowDetailDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\EscrowDetailDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `OrderId` | guid | optional |
| `BuyerId` | guid | optional |
| `SellerId` | guid | optional |
| `Amount` | number | required |
| `Currency` | string | optional |
| `Status` | string | optional |
| `HoldTransactionId` | guid | optional |
| `ReleaseTransactionId` | guid | optional |
| `ReleasedTo` | string | optional |
| `CreatedAt` | datetime | optional |
| `ReleasedAt` | datetime | optional |
| `ReleaseEvents` | [EscrowReleaseEventDto](./schemas.md#schema-escrowreleaseeventdto)[] | required |

<a id="schema-escrowdto"></a>
## EscrowDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\EscrowDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `OrderId` | guid | optional |
| `BuyerId` | guid | optional |
| `SellerId` | guid | optional |
| `Amount` | number | required |
| `Currency` | string | optional |
| `Status` | string | optional |
| `HoldTransactionId` | guid | optional |
| `CreatedAt` | datetime | optional |
| `ReleasedAt` | datetime | optional |
| `RefundedAt` | datetime | optional |

<a id="schema-escrowreleaseeventdto"></a>
## EscrowReleaseEventDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\EscrowReleaseEventDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ReleaseType` | string | optional |
| `TriggerSourceType` | string | optional |
| `TriggerSourceId` | guid | optional |
| `Amount` | number | required |
| `CreatedBy` | guid | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-flagauctionendpoint-request"></a>
## FlagAuctionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\FlagAuctionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AlertType` | string | optional |
| `Severity` | string | default = "medium" |
| `Payload` | string | default = "{}" |

<a id="schema-flaguserendpoint-request"></a>
## FlagUserEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\FlagUserEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FlagType` | string | optional |
| `Reason` | string | optional |
| `Severity` | string | default = "medium" |

<a id="schema-forgotpasswordendpoint-request"></a>
## ForgotPasswordEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\ForgotPasswordEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Email` | string | optional |

<a id="schema-getactivesessionsendpoint-parameters"></a>
## GetActiveSessionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetActiveSessionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `DeviceId` | guid | optional |

<a id="schema-getaddressesendpoint-parameters"></a>
## GetAddressesEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetAddressesEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-getallactivecategoriresfilterparameters"></a>
## GetAllActiveCategoriresFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetAllActiveCategories\GetAllActiveCategoriresFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getallcategoriesendpoint-parameters"></a>
## GetAllCategoriesEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Categories\GetAllCategoriesEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getauctionbidsendpoint-parameters"></a>
## GetAuctionBidsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\GetAuctionBidsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getauctionbidsfilterparameters"></a>
## GetAuctionBidsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetAuctionBids\GetAuctionBidsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getauctionsendpoint-parameters"></a>
## GetAuctionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\GetAuctionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `CategoryId` | guid | optional |
| `Search` | string | optional |
| `MinPrice` | number | optional |
| `MaxPrice` | number | optional |
| `SortBy` | string | optional |
| `EndingWithinHours` | number | optional |
| `IsFeatured` | boolean | optional |

<a id="schema-getauctionsfilterparameters"></a>
## GetAuctionsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetAuctions\GetAuctionsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `CategoryId` | guid | optional |
| `Search` | string | optional |
| `MinPrice` | number | optional |
| `MaxPrice` | number | optional |
| `SortBy` | string | optional |
| `EndingWithinHours` | number | optional |
| `IsFeatured` | boolean | optional |

<a id="schema-getcategorychildrenendpoint-parameters"></a>
## GetCategoryChildrenEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Categories\GetCategoryChildrenEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getcategorychildrenfilterparameters"></a>
## GetCategoryChildrenFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetCategoryChildren\GetCategoryChildrenFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getescrowsendpoint-parameters"></a>
## GetEscrowsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\Admins\GetEscrowsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `OrderId` | guid | optional |
| `BuyerId` | guid | optional |
| `SellerId` | guid | optional |

<a id="schema-getloginhistoryendpoint-parameters"></a>
## GetLoginHistoryEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetLoginHistoryEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-getmyauctionsendpoint-parameters"></a>
## GetMyAuctionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetMyAuctionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmyauctionsfilterparameters"></a>
## GetMyAuctionsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetMyAuctions\GetMyAuctionsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmybidsendpoint-parameters"></a>
## GetMyBidsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetMyBidsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmybidsfilterparameters"></a>
## GetMyBidsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetMyBids\GetMyBidsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmyitemsendpoint-parameters"></a>
## GetMyItemsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\GetMyItemsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getmyitemsfilterparameters"></a>
## GetMyItemsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetMyItems\GetMyItemsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getmynotificationsendpoint-parameters"></a>
## GetMyNotificationsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\NotificationContext\GetMyNotificationsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-getmywallettransactionsendpoint-parameters"></a>
## GetMyWalletTransactionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetMyWalletTransactionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Type` | string | optional |
| `From` | datetime | optional |
| `To` | datetime | optional |

<a id="schema-getmywatchlistendpoint-parameters"></a>
## GetMyWatchlistEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetMyWatchlistEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `AuctionStatus` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmywatchlistfilterparameters"></a>
## GetMyWatchlistFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetMyWatchlist\GetMyWatchlistFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `AuctionStatus` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getmywithdrawalsendpoint-parameters"></a>
## GetMyWithdrawalsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\GetMyWithdrawalsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |

<a id="schema-getpermissionsendpoint-parameters"></a>
## GetPermissionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\GetPermissionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Search` | string | optional |

<a id="schema-getpublicitemquestionsendpoint-parameters"></a>
## GetPublicItemQuestionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\GetPublicItemQuestionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getpublicitemquestionsfilterparameters"></a>
## GetPublicItemQuestionsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetPublicItemQuestions\GetPublicItemQuestionsFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `SortBy` | string | optional |

<a id="schema-getpublicselleritemsfilterparameters"></a>
## GetPublicSellerItemsFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetPublicSellerItems\GetPublicSellerItemsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-getreviewqueueendpoint-parameters"></a>
## GetReviewQueueEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\GetReviewQueueEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `AssignedAdminId` | guid | optional |

<a id="schema-getreviewqueuequeryfilterparameters"></a>
## GetReviewQueueQueryFilterParameters

- Source: .\src\core\OIO.Application\Context\AuctionContext\Queries\GetReviewQueue\GetReviewQueueQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `AssignedAdminId` | guid | optional |

<a id="schema-getselleritemsendpoint-parameters"></a>
## GetSellerItemsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Sellers\GetSellerItemsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-gettransactionsendpoint-parameters"></a>
## GetTransactionsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\Admins\GetTransactionsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `Type` | string | optional |
| `UserId` | guid | optional |
| `OrderId` | guid | optional |

<a id="schema-getusersendpoint-parameters"></a>
## GetUsersEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\GetUsersEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Search` | string | optional |
| `Status` | string | optional |
| `Role` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getusersfilterparameters"></a>
## GetUsersFilterParameters

- Source: .\src\core\OIO.Application\Context\UserContext\Queries\GetUsers\GetUsersFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Search` | string | optional |
| `Status` | string | optional |
| `Role` | string | optional |
| `SortBy` | string | optional |

<a id="schema-getwithdrawalsendpoint-parameters"></a>
## GetWithdrawalsEndpoint.Parameters

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\Admins\GetWithdrawalsEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |
| `UserId` | guid | optional |

<a id="schema-guid"></a>
## Guid

- Kieu du lieu co ban: guid

<a id="schema-hubcommandresult"></a>
## HubCommandResult

- Source: .\src\presentation\OIO.Api\Hubs\HubCommandResult.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Success` | boolean | required |
| `Data` | [T?](./schemas.md#schema-t) | optional |
| `Error` | [ErrorNotification?](./schemas.md#schema-errornotification) | optional |

<a id="schema-hubcommandresult-biddto"></a>
## HubCommandResult<BidDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| success | bool | Ket qua thuc thi command realtime |
| data | [B](./schemas.md#schema-b) | Payload thanh cong |
| error | [ErrorNotification](./schemas.md#schema-errornotification) | Thong tin loi neu success = false |

<a id="schema-hubcommandresult-buynowcheckoutdto"></a>
## HubCommandResult<BuyNowCheckoutDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| success | bool | Ket qua thuc thi command realtime |
| data | [B](./schemas.md#schema-b) | Payload thanh cong |
| error | [ErrorNotification](./schemas.md#schema-errornotification) | Thong tin loi neu success = false |

<a id="schema-inboundshipmentdto"></a>
## InboundShipmentDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\InboundShipmentDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ItemId` | guid | optional |
| `SellerId` | guid | optional |
| `ProviderCode` | string | optional |
| `ClientOrderCode` | string | optional |
| `CarrierTrackingNumber` | string | optional |
| `SenderName` | string | optional |
| `SenderPhone` | string | optional |
| `SenderAddress` | string | optional |
| `SenderWard` | string | optional |
| `SenderDistrict` | string | optional |
| `SenderProvince` | string | optional |
| `WeightGrams` | number | required |
| `LengthCm` | number | optional |
| `WidthCm` | number | optional |
| `HeightCm` | number | optional |
| `ShippingFee` | number | required |
| `InsuranceValue` | number | required |
| `Status` | string | optional |
| `Notes` | string | optional |
| `ExpectedArrivalAt` | datetime | optional |
| `ArrivedAt` | datetime | optional |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |
| `TrackingEvents` | [ShipmentTrackingEventDto](./schemas.md#schema-shipmenttrackingeventdto)[] | required |

<a id="schema-inspectionqueueitemdto"></a>
## InspectionQueueItemDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\WarehouseInspectionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `InboundShipmentId` | guid | optional |
| `ItemId` | guid | optional |
| `ItemTitle` | string | optional |
| `SellerId` | guid | optional |
| `WarehouseItemId` | guid | optional |
| `InspectionId` | guid | optional |
| `ShipmentStatus` | string | optional |
| `QueueStatus` | string | optional |
| `CarrierTrackingNumber` | string | optional |
| `ArrivedAt` | datetime | optional |
| `DeclaredCondition` | string | optional |
| `ConditionOnArrival` | string | optional |
| `InspectedAt` | datetime | optional |

<a id="schema-inspectwarehouseitemendpoint-request"></a>
## InspectWarehouseItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\InspectWarehouseItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ConditionId` | string | optional |
| `InspectionNotes` | string | optional |
| `InspectionMediaUploadIds` | [Guid](./schemas.md#schema-guid)[] | required |

<a id="schema-int"></a>
## int

- Kieu du lieu co ban: number

<a id="schema-ireadonlycollection-sellerprofiledto"></a>
## IReadOnlyCollection<SellerProfileDto>

- Collection payload
- Phan tu: [SellerProfileDto](./schemas.md#schema-sellerprofiledto)

<a id="schema-ireadonlycollection-t"></a>
## IReadOnlyCollection<T>

- Collection payload
- Phan tu: [T](./schemas.md#schema-t)

<a id="schema-ireadonlycollection-verificationdocumentdto"></a>
## IReadOnlyCollection<VerificationDocumentDto>

- Collection payload
- Phan tu: [VerificationDocumentDto](./schemas.md#schema-verificationdocumentdto)

<a id="schema-ireadonlycollection-verificationsummarydto"></a>
## IReadOnlyCollection<VerificationSummaryDto>

- Collection payload
- Phan tu: [VerificationSummaryDto](./schemas.md#schema-verificationsummarydto)

<a id="schema-ireadonlylist-disputemessageattachmentdto"></a>
## IReadOnlyList<DisputeMessageAttachmentDto>

- Collection payload
- Phan tu: [DisputeMessageAttachmentDto](./schemas.md#schema-disputemessageattachmentdto)

<a id="schema-ireadonlylist-disputemessagedto"></a>
## IReadOnlyList<DisputeMessageDto>

- Collection payload
- Phan tu: [DisputeMessageDto](./schemas.md#schema-disputemessagedto)

<a id="schema-ireadonlylist-disputeparticipantdto"></a>
## IReadOnlyList<DisputeParticipantDto>

- Collection payload
- Phan tu: [DisputeParticipantDto](./schemas.md#schema-disputeparticipantdto)

<a id="schema-ireadonlylist-escrowreleaseeventdto"></a>
## IReadOnlyList<EscrowReleaseEventDto>

- Collection payload
- Phan tu: [EscrowReleaseEventDto](./schemas.md#schema-escrowreleaseeventdto)

<a id="schema-ireadonlylist-guid"></a>
## IReadOnlyList<Guid>

- Collection payload
- Phan tu: [Guid](./schemas.md#schema-guid)

<a id="schema-ireadonlylist-guid"></a>
## IReadOnlyList<Guid>?

- Collection payload
- Phan tu: [Guid](./schemas.md#schema-guid)

<a id="schema-ireadonlylist-inboundshipmentdto"></a>
## IReadOnlyList<InboundShipmentDto>

- Collection payload
- Phan tu: [InboundShipmentDto](./schemas.md#schema-inboundshipmentdto)

<a id="schema-ireadonlylist-inspectionqueueitemdto"></a>
## IReadOnlyList<InspectionQueueItemDto>

- Collection payload
- Phan tu: [InspectionQueueItemDto](./schemas.md#schema-inspectionqueueitemdto)

<a id="schema-ireadonlylist-itemmediadto"></a>
## IReadOnlyList<ItemMediaDto>

- Collection payload
- Phan tu: [ItemMediaDto](./schemas.md#schema-itemmediadto)

<a id="schema-ireadonlylist-mediaattachment"></a>
## IReadOnlyList<MediaAttachment>?

- Collection payload
- Phan tu: [MediaAttachment](./schemas.md#schema-mediaattachment)

<a id="schema-ireadonlylist-monitoringalertdto"></a>
## IReadOnlyList<MonitoringAlertDto>

- Collection payload
- Phan tu: [MonitoringAlertDto](./schemas.md#schema-monitoringalertdto)

<a id="schema-ireadonlylist-orderdto"></a>
## IReadOnlyList<OrderDto>

- Collection payload
- Phan tu: [OrderDto](./schemas.md#schema-orderdto)

<a id="schema-ireadonlylist-outboundshipmentdto"></a>
## IReadOnlyList<OutboundShipmentDto>

- Collection payload
- Phan tu: [OutboundShipmentDto](./schemas.md#schema-outboundshipmentdto)

<a id="schema-ireadonlylist-paymentmethoddto"></a>
## IReadOnlyList<PaymentMethodDto>

- Collection payload
- Phan tu: [PaymentMethodDto](./schemas.md#schema-paymentmethoddto)

<a id="schema-ireadonlylist-reportdto"></a>
## IReadOnlyList<ReportDto>

- Collection payload
- Phan tu: [ReportDto](./schemas.md#schema-reportdto)

<a id="schema-ireadonlylist-roledto"></a>
## IReadOnlyList<RoleDto>

- Collection payload
- Phan tu: [RoleDto](./schemas.md#schema-roledto)

<a id="schema-ireadonlylist-shipmenttrackingeventdto"></a>
## IReadOnlyList<ShipmentTrackingEventDto>

- Collection payload
- Phan tu: [ShipmentTrackingEventDto](./schemas.md#schema-shipmenttrackingeventdto)

<a id="schema-ireadonlylist-storagelocationdto"></a>
## IReadOnlyList<StorageLocationDto>

- Collection payload
- Phan tu: [StorageLocationDto](./schemas.md#schema-storagelocationdto)

<a id="schema-ireadonlylist-string"></a>
## IReadOnlyList<string>

- Collection payload
- Phan tu: [string](./schemas.md#schema-string)

<a id="schema-ireadonlylist-systemsettingdto"></a>
## IReadOnlyList<SystemSettingDto>

- Collection payload
- Phan tu: [SystemSettingDto](./schemas.md#schema-systemsettingdto)

<a id="schema-ireadonlylist-termsacceptancedto"></a>
## IReadOnlyList<TermsAcceptanceDto>

- Collection payload
- Phan tu: [TermsAcceptanceDto](./schemas.md#schema-termsacceptancedto)

<a id="schema-ireadonlylist-termsdocumentdto"></a>
## IReadOnlyList<TermsDocumentDto>

- Collection payload
- Phan tu: [TermsDocumentDto](./schemas.md#schema-termsdocumentdto)

<a id="schema-ireadonlylist-warehouseinspectionevidencedto"></a>
## IReadOnlyList<WarehouseInspectionEvidenceDto>

- Collection payload
- Phan tu: [WarehouseInspectionEvidenceDto](./schemas.md#schema-warehouseinspectionevidencedto)

<a id="schema-ireadonlylist-warehouseitemdto"></a>
## IReadOnlyList<WarehouseItemDto>

- Collection payload
- Phan tu: [WarehouseItemDto](./schemas.md#schema-warehouseitemdto)

<a id="schema-itemdto"></a>
## ItemDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\ItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `SellerId` | guid | optional |
| `CategoryId` | guid | optional |
| `Title` | string | optional |
| `Description` | string | optional |
| `Condition` | string | optional |
| `Status` | string | optional |
| `Quantity` | number | required |
| `Images` | [ItemMediaDto](./schemas.md#schema-itemmediadto)[] | required |
| `CreatedAt` | datetime | optional |

<a id="schema-itemmediadto"></a>
## ItemMediaDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\ItemMediaDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Url` | string | optional |
| `PublicId` | string | optional |
| `ResourceType` | string | optional |
| `IsPrimary` | boolean | required |
| `SortOrder` | number | required |
| `FileName` | string | optional |
| `Bytes` | number | optional |
| `Format` | string | optional |
| `Width` | number | optional |
| `Height` | number | optional |
| `DurationSeconds` | number | optional |

<a id="schema-list-guid"></a>
## List<Guid>

- Collection payload
- Phan tu: [Guid](./schemas.md#schema-guid)

<a id="schema-list-mediaattachmentrequest"></a>
## List<MediaAttachmentRequest>?

- Collection payload
- Phan tu: [MediaAttachmentRequest](./schemas.md#schema-mediaattachmentrequest)

<a id="schema-loginhistorydto"></a>
## LoginHistoryDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\LoginHistoryDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `IpAddress` | string | optional |
| `UserAgent` | string | optional |
| `LoginAt` | datetime | optional |
| `Status` | string | optional |

<a id="schema-loginuserendpoint-request"></a>
## LoginUserEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\LoginUserEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Account` | string | required |
| `Password` | string | required |
| `DeviceId` | guid | optional |

<a id="schema-logoutuserendpoint-request"></a>
## LogoutUserEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\LogoutUserEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `DeviceId` | guid | default = null |

<a id="schema-markdisputereadendpoint-request"></a>
## MarkDisputeReadEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Disputes\MarkDisputeReadEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `LastReadMessageId` | guid | optional |

<a id="schema-mediaattachment"></a>
## MediaAttachment

- Source: .\src\core\OIO.Application\Context\AuctionContext\Commands\CreateItem\CreateItemCommand.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MediaUploadId` | guid | optional |
| `IsPrimary` | boolean | default = false |
| `SortOrder` | number | default = 0 |

<a id="schema-mediaattachmentrequest"></a>
## MediaAttachmentRequest

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\CreateItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MediaUploadId` | guid | optional |
| `PublicId` | string | optional |
| `IsPrimary` | boolean | default = false |
| `SortOrder` | number | default = 0 |

<a id="schema-metadata"></a>
## Metadata

- Source: .\src\core\OIO.Application\Abstractions\Commons\MetaData.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `CurrentPage` | number | required |
| `TotalPages` | number | required |
| `PageSize` | number | required |
| `TotalCount` | number | required |

<a id="schema-moneydto"></a>
## MoneyDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\MoneyDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |
| `Currency` | string | optional |
| `Symbol` | string | optional |

<a id="schema-moneydto"></a>
## MoneyDto?

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\MoneyDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |
| `Currency` | string | optional |
| `Symbol` | string | optional |

<a id="schema-monitoringalertdto"></a>
## MonitoringAlertDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\MonitoringAlertDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `EntityType` | string | optional |
| `EntityId` | guid | optional |
| `AlertType` | string | optional |
| `Severity` | string | optional |
| `Payload` | string | optional |
| `Status` | string | optional |
| `Notes` | string | optional |
| `AcknowledgedBy` | guid | optional |
| `AcknowledgedAt` | datetime | optional |
| `ResolvedBy` | guid | optional |
| `ResolvedAt` | datetime | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-myauctionwatchlistdto"></a>
## MyAuctionWatchlistDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\MyWatchlistDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `ItemTitle` | string | optional |
| `PrimaryImageUrl` | string | optional |
| `CurrentPrice` | number | required |
| `Currency` | string | optional |
| `AuctionStatus` | string | optional |
| `BidCount` | number | required |
| `EndTime` | datetime | optional |
| `RemainingTime` | duration | optional |
| `NotifyOnBid` | boolean | required |
| `NotifyOnEnd` | boolean | required |
| `WatchedAt` | datetime | optional |

<a id="schema-mybiddto"></a>
## MyBidDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\MyBidDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `AuctionId` | guid | optional |
| `ItemTitle` | string | optional |
| `PrimaryImageUrl` | string | optional |
| `Amount` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `CurrentPrice` | [MoneyDto](./schemas.md#schema-moneydto) | required |
| `Status` | string | optional |
| `AuctionStatus` | string | optional |
| `IsHighestBid` | boolean | required |
| `BidPlacedAt` | datetime | optional |
| `AuctionEndTime` | datetime | optional |

<a id="schema-notificationdto"></a>
## NotificationDto

- Source: .\src\core\OIO.Application\Context\NotificationContext\Queries\GetMyNotifications\NotificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `NotificationType` | string | optional |
| `EventType` | string | optional |
| `Title` | string | optional |
| `Message` | string | optional |
| `Priority` | string | optional |
| `Status` | string | optional |
| `EntityType` | string | optional |
| `EntityId` | guid | optional |
| `Metadata` | string | optional |
| `RelatedEntities` | string | optional |
| `Actions` | string | optional |
| `CreatedAt` | datetime | optional |
| `ReadAt` | datetime | optional |
| `ExpiresAt` | datetime | optional |

<a id="schema-notificationpushdto"></a>
## NotificationPushDto

- Source: .\src\core\OIO.Application\Context\NotificationContext\Hubs\INotificationHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `NotificationId` | guid | optional |
| `NotificationType` | string | optional |
| `EventType` | string | optional |
| `Title` | string | optional |
| `Message` | string | optional |
| `EntityType` | string | optional |
| `EntityId` | guid | optional |
| `Priority` | string | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-orderdto"></a>
## OrderDto

- Source: .\src\core\OIO.Application\Context\OrderContext\DTOs\OrderDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `OrderNumber` | string | optional |
| `AuctionId` | guid | optional |
| `BuyerId` | guid | optional |
| `SellerId` | guid | optional |
| `Status` | string | optional |
| `TotalAmount` | number | required |
| `Currency` | string | optional |
| `CreatedAt` | datetime | optional |
| `PaymentDueAt` | datetime | optional |
| `PaidAt` | datetime | optional |
| `ShippedAt` | datetime | optional |
| `DeliveredAt` | datetime | optional |
| `DecisionWindowEndsAt` | datetime | optional |
| `CompletedAt` | datetime | optional |
| `CancelledAt` | datetime | optional |
| `EscrowStatus` | string | optional |
| `TrackingNumber` | string | optional |
| `Return` | [OrderReturnDto?](./schemas.md#schema-orderreturndto) | optional |

<a id="schema-orderreturndto"></a>
## OrderReturnDto

- Source: .\src\core\OIO.Application\Context\OrderContext\DTOs\OrderReturnDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Status` | string | optional |
| `ReasonCode` | string | optional |
| `Description` | string | optional |
| `DecisionReason` | string | optional |
| `ProviderCode` | string | optional |
| `TrackingNumber` | string | optional |
| `RequestedAt` | datetime | optional |
| `ApprovedAt` | datetime | optional |
| `RejectedAt` | datetime | optional |
| `ShippedAt` | datetime | optional |
| `SellerReceivedAt` | datetime | optional |
| `BuyerDecisionDueAt` | datetime | optional |

<a id="schema-orderreturndto"></a>
## OrderReturnDto?

- Source: .\src\core\OIO.Application\Context\OrderContext\DTOs\OrderReturnDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Status` | string | optional |
| `ReasonCode` | string | optional |
| `Description` | string | optional |
| `DecisionReason` | string | optional |
| `ProviderCode` | string | optional |
| `TrackingNumber` | string | optional |
| `RequestedAt` | datetime | optional |
| `ApprovedAt` | datetime | optional |
| `RejectedAt` | datetime | optional |
| `ShippedAt` | datetime | optional |
| `SellerReceivedAt` | datetime | optional |
| `BuyerDecisionDueAt` | datetime | optional |

<a id="schema-outbidnotification"></a>
## OutbidNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `NewHighAmount` | number | required |
| `MinimumNextBid` | number | required |
| `NewHighBidderDisplayName` | string | optional |

<a id="schema-outboundshipmentdto"></a>
## OutboundShipmentDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\OutboundShipmentDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `OrderId` | guid | optional |
| `WarehouseItemId` | guid | optional |
| `ProviderCode` | string | optional |
| `ClientOrderCode` | string | optional |
| `CarrierTrackingNumber` | string | optional |
| `ShippingLabelUrl` | string | optional |
| `ShippingMethod` | string | optional |
| `WeightGrams` | number | required |
| `LengthCm` | number | optional |
| `WidthCm` | number | optional |
| `HeightCm` | number | optional |
| `ShippingFee` | number | required |
| `InsuranceValue` | number | required |
| `CodAmount` | number | required |
| `Status` | string | optional |
| `EstimatedDeliveryAt` | datetime | optional |
| `PackedAt` | datetime | optional |
| `DispatchedAt` | datetime | optional |
| `DeliveredAt` | datetime | optional |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |
| `TrackingEvents` | [ShipmentTrackingEventDto](./schemas.md#schema-shipmenttrackingeventdto)[] | required |

<a id="schema-pagedlist"></a>
## PagedList

- Source: .\src\core\OIO.Application\Abstractions\Commons\PagedList.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Items` | [T](./schemas.md#schema-t)[] | required |
| `Metadata` | [Metadata](./schemas.md#schema-metadata) | required |

<a id="schema-pagedlist-auctionlistitemdto"></a>
## PagedList<AuctionListItemDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [A](./schemas.md#schema-a)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-disputesummarydto"></a>
## PagedList<DisputeSummaryDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [D](./schemas.md#schema-d)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-escrowdto"></a>
## PagedList<EscrowDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [E](./schemas.md#schema-e)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-loginhistorydto"></a>
## PagedList<LoginHistoryDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [L](./schemas.md#schema-l)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-myauctionwatchlistdto"></a>
## PagedList<MyAuctionWatchlistDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [M](./schemas.md#schema-m)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-mybiddto"></a>
## PagedList<MyBidDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [M](./schemas.md#schema-m)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-notificationdto"></a>
## PagedList<NotificationDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [N](./schemas.md#schema-n)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-paymenttransactiondto"></a>
## PagedList<PaymentTransactionDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [P](./schemas.md#schema-p)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-publicselleritemdto"></a>
## PagedList<PublicSellerItemDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [P](./schemas.md#schema-p)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-string"></a>
## PagedList<string>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [s](./schemas.md#schema-s)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-useraddressdto"></a>
## PagedList<UserAddressDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [U](./schemas.md#schema-u)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-userlistitemdto"></a>
## PagedList<UserListItemDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [U](./schemas.md#schema-u)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-usersessiondto"></a>
## PagedList<UserSessionDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [U](./schemas.md#schema-u)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-wallettransactiondto"></a>
## PagedList<WalletTransactionDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [W](./schemas.md#schema-w)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedlist-withdrawalrequestdto"></a>
## PagedList<WithdrawalRequestDto>

| Field | Type | Mo ta |
| --- | --- | --- |
| items | [W](./schemas.md#schema-w)[] | Danh sach phan tu trong trang hien tai |
| metadata | [Metadata](./schemas.md#schema-metadata) | Thong tin paging |

<a id="schema-pagedparameters"></a>
## PagedParameters

- Source: .\src\core\OIO.Application\Abstractions\Commons\RequestParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |

<a id="schema-paymentmethoddto"></a>
## PaymentMethodDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\PaymentMethodDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Type` | string | optional |
| `Provider` | string | optional |
| `LastFour` | string | optional |
| `ExpiryMonth` | number | optional |
| `ExpiryYear` | number | optional |
| `HolderName` | string | optional |
| `IsDefault` | boolean | required |
| `IsActive` | boolean | required |
| `CreatedAt` | datetime | optional |

<a id="schema-paymentsummarydto"></a>
## PaymentSummaryDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\PaymentSummaryDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `CompletedPayments` | number | required |
| `FailedPayments` | number | required |
| `WalletTopUps` | number | required |
| `WithdrawalPendingCount` | number | required |
| `HoldingEscrowCount` | number | required |
| `ReleasedEscrowTotal` | number | required |
| `RefundedEscrowTotal` | number | required |

<a id="schema-paymenttransactiondto"></a>
## PaymentTransactionDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\PaymentTransactionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `TransactionNumber` | string | optional |
| `UserId` | guid | optional |
| `OrderId` | guid | optional |
| `Type` | string | optional |
| `Amount` | number | required |
| `Fee` | number | required |
| `NetAmount` | number | required |
| `Currency` | string | optional |
| `Status` | string | optional |
| `GatewayProvider` | string | optional |
| `Description` | string | optional |
| `CreatedAt` | datetime | optional |
| `ProcessedAt` | datetime | optional |

<a id="schema-permissionfilterparameters"></a>
## PermissionFilterParameters

- Source: .\src\core\OIO.Application\Context\UserContext\Queries\GetPermissions\PermissionFilterParameters.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Search` | string | optional |

<a id="schema-placebidendpoint-request"></a>
## PlaceBidEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\PlaceBidEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |
| `Currency` | string | default = "VND" |

<a id="schema-priceupdatenotification"></a>
## PriceUpdateNotification

- Source: .\src\core\OIO.Application\Context\AuctionContext\Hubs\IAuctionHubClient.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `CurrentPrice` | number | required |
| `MinimumNextBid` | number | required |
| `TotalBids` | number | required |
| `RemainingTime` | duration | optional |

<a id="schema-problemdetails"></a>
## ProblemDetails

| Field | Type | Ghi chu |
| --- | --- | --- |
| title | string | Tieu de loi |
| status | number | HTTP status code |
| detail | string | Noi dung chi tiet |
| instance | string | Route hoac request instance |

<a id="schema-publicselleritemauctionsummarydto"></a>
## PublicSellerItemAuctionSummaryDto?

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\PublicSellerItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AuctionId` | guid | optional |
| `AuctionStatus` | string | optional |
| `AuctionType` | string | optional |
| `CurrentPrice` | number | required |
| `Currency` | string | optional |
| `StartTime` | datetime | optional |
| `EndTime` | datetime | optional |

<a id="schema-publicselleritemdto"></a>
## PublicSellerItemDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\PublicSellerItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `SellerId` | guid | optional |
| `CategoryId` | guid | optional |
| `Title` | string | optional |
| `Description` | string | optional |
| `Condition` | string | optional |
| `Status` | string | optional |
| `Quantity` | number | required |
| `Images` | [ItemMediaDto](./schemas.md#schema-itemmediadto)[] | required |
| `CreatedAt` | datetime | optional |
| `Auction` | [PublicSellerItemAuctionSummaryDto?](./schemas.md#schema-publicselleritemauctionsummarydto) | optional |
| `HasLiveAuction` | boolean | required |

<a id="schema-publicsellerprofiledto"></a>
## PublicSellerProfileDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\SellerProfileDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `StoreName` | string | optional |
| `StoreDescription` | string | optional |
| `Status` | string | optional |
| `TotalSalesCount` | number | required |
| `CreatedAt` | datetime | optional |

<a id="schema-refreshtokenendpoint-request"></a>
## RefreshTokenEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\RefreshTokenEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `RefreshToken` | string | required |
| `DeviceId` | guid | required |

<a id="schema-refundvnpayendpoint-request"></a>
## RefundVnPayEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\VnPay\RefundVnPayEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `OriginalTransactionRef` | string | optional |
| `OriginalVnPayTransactionNo` | string | optional |
| `Amount` | number | required |
| `Reason` | string | optional |

<a id="schema-registeruserendpoint-request"></a>
## RegisterUserEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\RegisterUserEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `UserName` | string | required |
| `Email` | string | required |
| `Password` | string | required |
| `Currency` | string | required |
| `FirstName` | string | default = null |
| `LastName` | string | default = null |

<a id="schema-rejectitemendpoint-request"></a>
## RejectItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\RejectItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-rejectorderreturnendpoint-request"></a>
## RejectOrderReturnEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\OrderContext\RejectOrderReturnEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-rejectverificationendpoint-request"></a>
## RejectVerificationEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\RejectVerificationEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | required |
| `RejectionCode` | string | default = null |

<a id="schema-rejectwithdrawalendpoint-request"></a>
## RejectWithdrawalEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\PaymentContext\Admins\RejectWithdrawalEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Reason` | string | optional |

<a id="schema-relistauctionendpoint-request"></a>
## RelistAuctionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\RelistAuctionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `QualificationStartAt` | datetime | optional |
| `QualificationEndAt` | datetime | optional |
| `StartAt` | datetime | optional |
| `EndAt` | datetime | optional |
| `Reason` | string | default = null |

<a id="schema-reorderitemmediaendpoint-request"></a>
## ReorderItemMediaEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\ReorderItemMediaEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `OrderedMediaIds` | [Guid](./schemas.md#schema-guid)[] | required |

<a id="schema-reportdto"></a>
## ReportDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\ReportDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ReporterId` | guid | optional |
| `EntityType` | string | optional |
| `EntityId` | guid | optional |
| `ReasonCode` | string | optional |
| `Description` | string | optional |
| `Attachments` | string | optional |
| `Status` | string | optional |
| `AssignedTo` | guid | optional |
| `CreatedAt` | datetime | optional |
| `AssignedAt` | datetime | optional |
| `ResolvedAt` | datetime | optional |
| `EscalatedEmergencyAt` | datetime | optional |
| `ResolutionNotes` | string | optional |

<a id="schema-requestuploadsignatureendpoint-request"></a>
## RequestUploadSignatureEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\MediaContext\Media\RequestUploadSignatureEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Context` | string | optional |
| `FileName` | string | optional |

<a id="schema-resendconfirmemailendpoint-request"></a>
## ResendConfirmEmailEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\ResendConfirmEmailEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Email` | string | optional |

<a id="schema-resetpasswordendpoint-request"></a>
## ResetPasswordEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Auth\ResetPasswordEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Email` | string | optional |
| `Token` | string | optional |
| `NewPassword` | string | optional |
| `ConfirmPassword` | string | optional |

<a id="schema-resolveauctionemergencyendpoint-request"></a>
## ResolveAuctionEmergencyEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\ResolveAuctionEmergencyEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Status` | string | optional |
| `Payload` | string | default = "{}" |

<a id="schema-resolvedisputeendpoint-request"></a>
## ResolveDisputeEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\ResolveDisputeEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ResolutionType` | string | optional |
| `Notes` | string | default = null |
| `Amount` | number | default = null |

<a id="schema-resolvemonitoringalertendpoint-request"></a>
## ResolveMonitoringAlertEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\ResolveMonitoringAlertEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Ignored` | boolean | required |
| `Notes` | string | optional |

<a id="schema-resolvereportendpoint-request"></a>
## ResolveReportEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Admins\ResolveReportEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Dismissed` | boolean | required |
| `ResolutionNotes` | string | optional |

<a id="schema-respondrunnerupofferendpoint-request"></a>
## RespondRunnerUpOfferEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\RespondRunnerUpOfferEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Accept` | boolean | required |

<a id="schema-resubmititemendpoint-request"></a>
## ResubmitItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\ResubmitItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `VerifyByPlatform` | boolean | default = false |

<a id="schema-reviewwarehouseinspectionendpoint-request"></a>
## ReviewWarehouseInspectionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\ReviewWarehouseInspectionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Decision` | string | optional |
| `Reason` | string | default = null |

<a id="schema-roledto"></a>
## RoleDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\RoleDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Name` | string | optional |
| `Permissions` | [string](./schemas.md#schema-string)[] | required |

<a id="schema-sellerprofiledto"></a>
## SellerProfileDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\SellerProfileDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `StoreName` | string | optional |
| `StoreDescription` | string | optional |
| `Status` | string | optional |
| `VerifiedAt` | datetime | optional |
| `TotalSalesCount` | number | required |
| `TotalSalesAmount` | number | required |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |

<a id="schema-senddisputemessageendpoint-request"></a>
## SendDisputeMessageEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\ModerationContext\Disputes\SendDisputeMessageEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Message` | string | optional |
| `MediaUploadIds` | [Guid](./schemas.md#schema-guid)[] | default = null |
| `IsInternal` | boolean | default = false |

<a id="schema-sessionexpirationdto"></a>
## SessionExpirationDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\SessionExpirationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `SessionId` | guid | optional |
| `DeviceId` | guid | optional |
| `SlidingExpiresAt` | datetime | optional |
| `AbsoluteExpiresAt` | datetime | optional |
| `IsNearingAbsoluteExpiration` | boolean | required |
| `RemainingAbsoluteTime` | duration | optional |

<a id="schema-setauctioncurationendpoint-request"></a>
## SetAuctionCurationEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\SetAuctionCurationEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `AssignedAdminId` | guid | default = null |
| `ClearAssignedAdmin` | boolean | default = false |
| `Priority` | number | default = null |
| `PriorityReason` | string | default = null |
| `IsFeatured` | boolean | default = null |

<a id="schema-setauctiontimingendpoint-request"></a>
## SetAuctionTimingEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\SetAuctionTimingEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StartTime` | datetime | optional |
| `EndTime` | datetime | optional |
| `QualificationStartAt` | datetime | optional |
| `QualificationEndAt` | datetime | optional |
| `AutoExtend` | boolean | default = true |
| `ExtensionMinutes` | number | default = 5 |

<a id="schema-setphonenumberendpoint-request"></a>
## SetPhoneNumberEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\SetPhoneNumberEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PhoneNumber` | string | required |
| `CountryCode` | string | default = PhoneNumber.DefaultRegion |

<a id="schema-shipmenttrackingeventdto"></a>
## ShipmentTrackingEventDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\ShipmentTrackingEventDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `CarrierStatusRaw` | string | optional |
| `CarrierStatusDesc` | string | optional |
| `NormalizedStatus` | string | optional |
| `Location` | string | optional |
| `ReasonCode` | string | optional |
| `ReasonDescription` | string | optional |
| `EventTime` | datetime | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-shiporderreturnendpoint-request"></a>
## ShipOrderReturnEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\OrderContext\ShipOrderReturnEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `ProviderCode` | string | optional |
| `TrackingNumber` | string | optional |

<a id="schema-storagelocationdto"></a>
## StorageLocationDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\StorageLocationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Zone` | string | optional |
| `Aisle` | string | optional |
| `Shelf` | string | optional |
| `Bin` | string | optional |
| `Label` | string | optional |
| `IsOccupied` | boolean | required |
| `CreatedAt` | datetime | optional |

<a id="schema-storewarehouseitemendpoint-request"></a>
## StoreWarehouseItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\StoreWarehouseItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StorageLocationId` | guid | optional |

<a id="schema-string"></a>
## string

- Kieu du lieu co ban: string

<a id="schema-string"></a>
## string?

- Kieu du lieu co ban: string

<a id="schema-submititemendpoint-request"></a>
## SubmitItemEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Items\SubmitItemEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `VerifyByPlatform` | boolean | default = false |

<a id="schema-submitsealedbidendpoint-request"></a>
## SubmitSealedBidEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\SubmitSealedBidEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Amount` | number | required |

<a id="schema-systemsettingdto"></a>
## SystemSettingDto

- Source: .\src\core\OIO.Application\Context\AdminContext\DTOs\SystemSettingDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Key` | string | optional |
| `Value` | object | optional |
| `ValueType` | string | optional |
| `Description` | string | optional |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |
| `ModifiedBy` | string | optional |

<a id="schema-togglepermissionfromroleendpoint-request"></a>
## TogglePermissionFromRoleEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\TogglePermissionFromRoleEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `IsActive` | boolean | required |

<a id="schema-triggerauctionemergencyendpoint-request"></a>
## TriggerAuctionEmergencyEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Admins\TriggerAuctionEmergencyEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `TriggerSource` | string | optional |
| `Reason` | string | optional |
| `Payload` | string | default = "{}" |

<a id="schema-updateaddressendpoint-request"></a>
## UpdateAddressEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\UpdateAddressEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Type` | string | optional |
| `RecipientName` | string | optional |
| `Street` | string | optional |
| `Ward` | string | optional |
| `District` | string | optional |
| `City` | string | optional |
| `PhoneNumber` | string | optional |
| `CountryCode` | string | optional |
| `PostalCode` | string | optional |

<a id="schema-updateauctionendpoint-request"></a>
## UpdateAuctionEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Auctions\UpdateAuctionEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StartingPrice` | number | default = null |
| `BidIncrement` | number | default = null |
| `ReservePrice` | number | default = null |
| `BuyNowPrice` | number | default = null |
| `Currency` | string | default = null |
| `AuctionType` | string | default = null |
| `StartTime` | datetime | default = null |
| `EndTime` | datetime | default = null |
| `QualificationStartAt` | datetime | default = null |
| `QualificationEndAt` | datetime | default = null |
| `AutoExtend` | boolean | default = null |
| `ExtensionMinutes` | number | default = null |

<a id="schema-updatecategoryendpoint-request"></a>
## UpdateCategoryEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\AuctionContext\Categories\UpdateCategoryEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Name` | string | default = null |
| `Slug` | string | default = null |
| `Description` | string | default = null |
| `IconUrl` | string | default = null |
| `IsActive` | boolean | default = null |
| `SortOrder` | number | default = null |

<a id="schema-updatecurrentuserprofileendpoint-request"></a>
## UpdateCurrentUserProfileEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\UpdateCurrentUserProfileEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FirstName` | string | optional |
| `LastName` | string | optional |
| `DisplayName` | string | optional |
| `AvatarUrl` | string | optional |
| `DateOfBirth` | date | optional |
| `Gender` | string | optional |

<a id="schema-updatesellerprofileendpoint-request"></a>
## UpdateSellerProfileEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\UpdateSellerProfileEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `StoreName` | string | required |
| `StoreDescription` | string | required |

<a id="schema-updateshippingproviderconfigendpoint-request"></a>
## UpdateShippingProviderConfigEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\UpdateShippingProviderConfigEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `DisplayName` | string | optional |
| `ApiBaseUrl` | string | optional |
| `PickName` | string | optional |
| `PickPhone` | string | optional |
| `PickAddress` | string | optional |
| `PickWard` | string | optional |
| `PickDistrict` | string | optional |
| `PickProvince` | string | optional |
| `PickCarrierAddressDataJson` | string | optional |
| `WebhookSecret` | string | optional |
| `CredentialsJson` | string | optional |

<a id="schema-updatestoragelocationendpoint-request"></a>
## UpdateStorageLocationEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\WarehouseContext\UpdateStorageLocationEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Zone` | string | optional |
| `Aisle` | string | optional |
| `Shelf` | string | optional |
| `Bin` | string | optional |

<a id="schema-updatesystemsettingendpoint-request"></a>
## UpdateSystemSettingEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Admins\UpdateSystemSettingEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Key` | string | optional |
| `Value` | string | optional |

<a id="schema-updateverificationendpoint-request"></a>
## UpdateVerificationEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\UpdateVerificationEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FullName` | string | required |
| `DateOfBirth` | date | required |
| `Gender` | string | required |
| `IdType` | string | required |
| `IdNumber` | string | required |
| `IdIssuedDate` | date | optional |
| `IdExpiredDate` | date | optional |
| `IdIssuedPlace` | string | optional |
| `FullAddress` | string | required |
| `Province` | string | required |
| `District` | string | required |
| `Ward` | string | required |
| `Nationality` | string | default = null |

<a id="schema-uploadverificationdocumentendpoint-request"></a>
## UploadVerificationDocumentEndpoint.Request

- Source: .\src\presentation\OIO.Api\Endpoints\UserContext\Me\UploadVerificationDocumentEndpoint.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `MediaUploadId` | guid | required |
| `DocumentType` | string | required |

<a id="schema-useraddressdto"></a>
## UserAddressDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserAddressDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Type` | string | optional |
| `RecipientName` | string | optional |
| `PhoneNumber` | string | optional |
| `Street` | string | optional |
| `Ward` | string | optional |
| `District` | string | optional |
| `City` | string | optional |
| `PostalCode` | string | optional |
| `IsDefault` | boolean | required |

<a id="schema-userdto"></a>
## UserDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `UserName` | string | optional |
| `Email` | string | optional |
| `EmailConfirmed` | boolean | required |
| `PhoneNumber` | string | optional |
| `CountryCode` | string | optional |
| `PhoneNumberConfirmed` | boolean | required |
| `TwoFactorEnabled` | boolean | required |
| `TwoFactorProvider` | string | optional |
| `Status` | string | optional |
| `CreatedAt` | datetime | optional |
| `Profile` | [UserProfileDto?](./schemas.md#schema-userprofiledto) | optional |

<a id="schema-userlistitemdto"></a>
## UserListItemDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserListItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `UserName` | string | optional |
| `Email` | string | optional |
| `FirstName` | string | optional |
| `LastName` | string | optional |
| `Status` | string | optional |
| `EmailConfirmed` | boolean | required |
| `Roles` | [string](./schemas.md#schema-string)[] | required |
| `CreatedAt` | datetime | optional |

<a id="schema-userprofiledto"></a>
## UserProfileDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserProfileDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FirstName` | string | optional |
| `LastName` | string | optional |
| `DisplayName` | string | optional |
| `FullName` | string | optional |
| `AvatarUrl` | string | optional |
| `DateOfBirth` | date | optional |
| `Gender` | string | optional |

<a id="schema-userprofiledto"></a>
## UserProfileDto?

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserProfileDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FirstName` | string | optional |
| `LastName` | string | optional |
| `DisplayName` | string | optional |
| `FullName` | string | optional |
| `AvatarUrl` | string | optional |
| `DateOfBirth` | date | optional |
| `Gender` | string | optional |

<a id="schema-userriskflagdto"></a>
## UserRiskFlagDto

- Source: .\src\core\OIO.Application\Context\ModerationContext\DTOs\UserRiskFlagDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `UserId` | guid | optional |
| `FlagType` | string | optional |
| `Reason` | string | optional |
| `Severity` | string | optional |
| `CreatedBy` | guid | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-usersessiondto"></a>
## UserSessionDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\UserSessionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `SessionId` | guid | optional |
| `DeviceId` | guid | optional |
| `UserAgent` | string | optional |
| `IpAddress` | string | optional |
| `IsActive` | boolean | required |
| `IsCurrentDevice` | boolean | required |
| `CreatedAt` | datetime | optional |
| `LastRotatedAt` | datetime | optional |
| `SlidingExpiresAt` | datetime | optional |
| `AbsoluteExpiresAt` | datetime | optional |
| `IsNearingAbsoluteExpiration` | boolean | required |
| `RemainingAbsoluteTime` | duration | optional |

<a id="schema-validationproblemdetails"></a>
## ValidationProblemDetails

| Field | Type | Ghi chu |
| --- | --- | --- |
| title | string | Tieu de loi |
| status | number | HTTP status code |
| detail | string | Noi dung chi tiet |
| instance | string | Route hoac request instance |
| errors | [Dictionary<string, string[]>](./schemas.md#schema-dictionary-string-string) | Tap loi validation theo field |

<a id="schema-verificationaddressdto"></a>
## VerificationAddressDto?

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\VerificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `FullAddress` | string | optional |
| `Province` | string | optional |
| `District` | string | optional |
| `Ward` | string | optional |

<a id="schema-verificationdocumentdto"></a>
## VerificationDocumentDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\VerificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `DocumentType` | string | optional |
| `ResourceType` | string | optional |
| `SecureUrl` | string | optional |
| `FileHash` | string | optional |
| `MimeType` | string | optional |
| `VerificationStatus` | string | optional |
| `UploadedAt` | datetime | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-verificationdocumentinfodto"></a>
## VerificationDocumentInfoDto?

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\VerificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `IdType` | string | optional |
| `IdNumber` | string | optional |
| `IssuedDate` | date | optional |
| `ExpiredDate` | date | optional |
| `IssuedPlace` | string | optional |

<a id="schema-verificationdto"></a>
## VerificationDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\VerificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `UserId` | guid | optional |
| `VerificationType` | string | optional |
| `AutoVerified` | boolean | required |
| `FullName` | string | optional |
| `DateOfBirth` | date | optional |
| `Gender` | string | optional |
| `Nationality` | string | optional |
| `Document` | [VerificationDocumentInfoDto?](./schemas.md#schema-verificationdocumentinfodto) | optional |
| `PermanentAddress` | [VerificationAddressDto?](./schemas.md#schema-verificationaddressdto) | optional |
| `Status` | string | optional |
| `VerifiedAt` | datetime | optional |
| `VerifiedBy` | guid | optional |
| `RejectionReason` | string | optional |
| `RejectionCode` | string | optional |
| `SubmittedAt` | datetime | optional |
| `ExpiresAt` | datetime | optional |
| `AttemptCount` | number | required |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |
| `Documents` | [VerificationDocumentDto](./schemas.md#schema-verificationdocumentdto)[] | required |

<a id="schema-verificationsummarydto"></a>
## VerificationSummaryDto

- Source: .\src\core\OIO.Application\Context\UserContext\DTOs\VerificationDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `VerificationType` | string | optional |
| `AutoVerified` | boolean | required |
| `FullName` | string | optional |
| `Status` | string | optional |
| `SubmittedAt` | datetime | optional |
| `AttemptCount` | number | required |
| `CreatedAt` | datetime | optional |

<a id="schema-walletsummarydto"></a>
## WalletSummaryDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\WalletSummaryDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `WalletId` | guid | optional |
| `Currency` | string | optional |
| `AvailableBalance` | number | required |
| `PendingBalance` | number | required |
| `TotalBalance` | number | required |
| `IsActive` | boolean | required |
| `UpdatedAt` | datetime | optional |

<a id="schema-wallettransactiondto"></a>
## WalletTransactionDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\WalletTransactionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Type` | string | optional |
| `Amount` | number | required |
| `Currency` | string | optional |
| `BalanceBefore` | number | required |
| `BalanceAfter` | number | required |
| `Description` | string | optional |
| `ReferenceType` | string | optional |
| `ReferenceId` | guid | optional |
| `CreatedAt` | datetime | optional |

<a id="schema-wallettransactionfilterparameters"></a>
## WalletTransactionFilterParameters

- Source: .\src\core\OIO.Application\Context\PaymentContext\Queries\GetMyWalletTransactions\GetMyWalletTransactionsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Type` | string | optional |
| `From` | datetime | optional |
| `To` | datetime | optional |

<a id="schema-warehouseinspectiondto"></a>
## WarehouseInspectionDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\WarehouseInspectionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `WarehouseItemId` | guid | optional |
| `InboundShipmentId` | guid | optional |
| `ItemId` | guid | optional |
| `DeclaredCondition` | string | optional |
| `ConditionOnArrival` | string | optional |
| `InspectionNotes` | string | optional |
| `DecisionStatus` | string | optional |
| `DecisionReason` | string | optional |
| `InspectedBy` | guid | optional |
| `InspectedAt` | datetime | optional |
| `ReviewedBy` | guid | optional |
| `ReviewedAt` | datetime | optional |
| `SellerConfirmedAt` | datetime | optional |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |
| `Evidence` | [WarehouseInspectionEvidenceDto](./schemas.md#schema-warehouseinspectionevidencedto)[] | required |

<a id="schema-warehouseinspectionevidencedto"></a>
## WarehouseInspectionEvidenceDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\WarehouseInspectionDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PublicId` | string | optional |
| `Folder` | string | optional |
| `SecureUrl` | string | optional |
| `FileName` | string | optional |
| `Bytes` | number | optional |
| `Format` | string | optional |
| `Width` | number | optional |
| `Height` | number | optional |
| `DurationSeconds` | number | optional |

<a id="schema-warehouseitemdto"></a>
## WarehouseItemDto

- Source: .\src\core\OIO.Application\Context\WarehouseContext\DTOs\WarehouseItemDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `ItemId` | guid | optional |
| `InboundShipmentId` | guid | optional |
| `StorageLocationId` | guid | optional |
| `Status` | string | optional |
| `ReceivedAt` | datetime | optional |
| `CreatedAt` | datetime | optional |
| `ModifiedAt` | datetime | optional |

<a id="schema-winnerofferdto"></a>
## WinnerOfferDto

- Source: .\src\core\OIO.Application\Context\AuctionContext\DTOs\WinnerOfferDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `AuctionId` | guid | optional |
| `UserId` | guid | optional |
| `RankNo` | number | required |
| `Status` | string | optional |
| `OfferedAt` | datetime | optional |
| `ExpiresAt` | datetime | optional |
| `RespondedAt` | datetime | optional |

<a id="schema-withdrawalfilterparameters"></a>
## WithdrawalFilterParameters

- Source: .\src\core\OIO.Application\Context\PaymentContext\Queries\GetMyWithdrawals\GetMyWithdrawalsQuery.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `PageNumber` | number | optional |
| `PageSize` | number | optional |
| `Status` | string | optional |

<a id="schema-withdrawalrequestdto"></a>
## WithdrawalRequestDto

- Source: .\src\core\OIO.Application\Context\PaymentContext\DTOs\WithdrawalRequestDto.cs

| Field | Type | Ghi chu |
| --- | --- | --- |
| `Id` | guid | optional |
| `Amount` | number | required |
| `Fee` | number | required |
| `NetAmount` | number | required |
| `Status` | string | optional |
| `BankName` | string | optional |
| `AccountNumberMasked` | string | optional |
| `AccountHolder` | string | optional |
| `RejectionReason` | string | optional |
| `CreatedAt` | datetime | optional |
| `ProcessedAt` | datetime | optional |

