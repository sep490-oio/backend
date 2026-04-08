using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.ReviewContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using Vogen;

namespace OIO.Infrastructure.Persistence.Converters;

//AuctionContext
[EfCoreConverter<AuctionDepositId>]
[EfCoreConverter<AuctionBuyNowReservationId>]
[EfCoreConverter<AuctionEmergencyActionId>]
[EfCoreConverter<AuctionId>]
[EfCoreConverter<AuctionParticipantId>]
[EfCoreConverter<AuctionPriceHistoryId>]
[EfCoreConverter<AuctionRelistHistoryId>]
[EfCoreConverter<AuctionWatcherId>]
[EfCoreConverter<AuctionWinnerOfferId>]
[EfCoreConverter<AutoBidId>]
[EfCoreConverter<BidEventId>]
[EfCoreConverter<BidId>]
[EfCoreConverter<SealedBidId>]

//CatalogContext
[EfCoreConverter<CategoryId>]
[EfCoreConverter<ItemId>]
[EfCoreConverter<ItemMediaId>]
[EfCoreConverter<ItemModerationReviewId>]
[EfCoreConverter<ItemQuestionId>]

//ModerationContext
[EfCoreConverter<AdminReviewTaskId>]
[EfCoreConverter<AuditLogId>]
[EfCoreConverter<DisputeEvidenceId>]
[EfCoreConverter<DisputeId>]
[EfCoreConverter<DisputeMessageAttachmentId>]
[EfCoreConverter<DisputeMessageId>]
[EfCoreConverter<DisputeParticipantStateId>]
[EfCoreConverter<DisputeRefundId>]
[EfCoreConverter<DisputeResponseTemplateId>]
[EfCoreConverter<DisputeStatusHistoryId>]
[EfCoreConverter<MonitoringAlertId>]
[EfCoreConverter<ReportId>]
[EfCoreConverter<ReviewQueueId>]

//NotficationContext
[EfCoreConverter<NotificationDeliveryId>]
[EfCoreConverter<NotificationId>]

//OrderContext
[EfCoreConverter<OrderId>]
[EfCoreConverter<OrderReturnId>]
[EfCoreConverter<SellerDirectShipmentId>]
[EfCoreConverter<SellerDirectShipmentEvidenceId>]

//PaymentContext
[EfCoreConverter<EscrowId>]
[EfCoreConverter<EscrowReleaseEventId>]
[EfCoreConverter<InvoiceId>]
[EfCoreConverter<PaymentMethodId>]
[EfCoreConverter<TransactionId>]
[EfCoreConverter<WalletId>]
[EfCoreConverter<WalletTransactionId>]
[EfCoreConverter<WithdrawalRequestId>]
[EfCoreConverter<GatewayWebhookEventId>]

//ReviewContext
[EfCoreConverter<BuyerReviewId>]
[EfCoreConverter<ReviewImageId>]
[EfCoreConverter<ReviewReportId>]
[EfCoreConverter<ReviewVoteId>]
[EfCoreConverter<SellerRatingSummaryId>]
[EfCoreConverter<SellerReviewId>]

//Shared
[EfCoreConverter<MediaUploadId>]

//ShippingContext
[EfCoreConverter<InboundShipmentId>]
[EfCoreConverter<OutboundShipmentId>]
[EfCoreConverter<ShipmentTrackingEventId>]
[EfCoreConverter<ShippingProviderConfigId>]
[EfCoreConverter<WarehouseInspectionId>]
[EfCoreConverter<WarehouseItemId>]
[EfCoreConverter<WarehouseItemMediaId>]
[EfCoreConverter<WarehouseStorageLocationId>]

//UserContext
[EfCoreConverter<IdentityVerificationId>]
[EfCoreConverter<SellerKycDocumentId>]
[EfCoreConverter<SellerKycHistoryId>]
[EfCoreConverter<SellerKycId>]
[EfCoreConverter<SellerProfileId>]
[EfCoreConverter<TermsAcceptanceId>]
[EfCoreConverter<TermsDocumentId>]
[EfCoreConverter<UserAddressId>]
[EfCoreConverter<UserId>]
[EfCoreConverter<UserLoginHistoryId>]
[EfCoreConverter<UserNotificationPreferenceId>]
[EfCoreConverter<UserRefreshTokenId>]
[EfCoreConverter<UserRiskFlagId>]
[EfCoreConverter<UserSessionId>]
[EfCoreConverter<VerificationDocumentId>]
[EfCoreConverter<VerificationHistoryId>]
public partial class EfCoreConverters;
