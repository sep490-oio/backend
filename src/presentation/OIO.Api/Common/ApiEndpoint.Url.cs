namespace OIO.Api.Common;

public static partial class ApiEndpoint
{
    public static class Url
    {
        public static class Search
        {
            public const string Global = "api/search";
            public const string Suggestions = $"{Global}/suggestions";
            public const string Auctions = $"{Global}/auctions";
            public const string MySearch = "api/me/search";
            public const string Sync = "api/search/sync";
            public const string Items = $"{Global}/items";
            public const string Orders = $"{Global}/orders";
            public const string Users = $"{Global}/users";
            public const string Shipments = $"{Global}/shipments";
            public const string Warehouse = $"{Global}/warehouse";
        }

        public static class Media
        {
            private const string Base = "api/media";

            public const string RequestSignature = $"{Base}/upload-signature";
            public const string Confirm = $"{Base}/confirm";
            public const string Contexts = $"{Base}/contexts";
            public const string BatchRequestSignature = $"{Base}/batch-upload-signatures";
            public const string BatchConfirm = $"{Base}/batch-confirm";
        }
        
        public static class Terms
        {
            private const string Base = "api/terms";

            public const string GetActive = $"{Base}/active";
            public const string GetActiveByType = $"{Base}/{{type}}/active";
            public const string GetById = $"{Base}/{{termId:guid}}";
        }

        public static class Admins
        {
            private const string Base = "api/admin";

            public const string CreateUser = $"{Base}/users";
            public const string GetUsers = $"{Base}/users";
            public const string GetRoles = $"{Base}/roles";
            public const string GetPermissions = $"{Base}/permissions";
            public const string GetUser = $"{Base}/users/{{userId:guid}}";
            public const string GetAllTerms = $"{Base}/terms";
            public const string CreateTerms = $"{Base}/terms";
            public const string UpdateTerms = $"{Base}/terms/{{id:guid}}";
            public const string ActivateTerms = $"{Base}/terms/{{id:guid}}/activate";
            public const string ArchiveTerms = $"{Base}/terms/{{id:guid}}/archive";
            public const string DeleteTerms = $"{Base}/terms/{{id:guid}}";

            
            public const string RevokeRole = $"{Base}/users/{{userId:guid}}/roles/{{role}}";
            public const string RevokePermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string RemoveUser = $"{Base}/users/{{userId:guid}}";

            public const string AssignRole = $"{Base}/users/{{userId:guid}}/roles/{{role}}";
            public const string ChangeUserStatus = $"{Base}/users/{{userId:guid}}/status";
            public const string GrantPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string DenyPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string UnlockUser = $"{Base}/users/{{userId:guid}}/unlock";
            public const string TogglePermission = $"{Base}/roles/{{role}}/permissions/{{permission}}";

            // Verifications
            public const string GetPendingVerifications = $"{Base}/verifications";
            public const string GetVerificationById = $"{Base}/verifications/{{verificationId:guid}}";
            public const string ApproveVerification = $"{Base}/verifications/{{verificationId:guid}}/approve";
            public const string RejectVerification = $"{Base}/verifications/{{verificationId:guid}}/reject";

            // Seller Profiles
            public const string GetSellerProfiles = $"{Base}/seller-profiles";
            public const string VerifySellerProfile = $"{Base}/seller-profiles/{{id:guid}}/verify";
            public const string RejectSellerProfile = $"{Base}/seller-profiles/{{id:guid}}/reject";

            // Disputes
            public const string GetAdminDisputes = $"{Base}/disputes";
            public const string GetAdminDisputeById = $"{Base}/disputes/{{disputeId:guid}}";
            public const string AssignDispute = $"{Base}/disputes/{{disputeId:guid}}/assign";
            public const string TransitionDisputeStatus = $"{Base}/disputes/{{disputeId:guid}}/transition";
            public const string RequestDisputeEvidence = $"{Base}/disputes/{{disputeId:guid}}/request-evidence";
            public const string AddDisputeFinding = $"{Base}/disputes/{{disputeId:guid}}/findings";
            public const string ResolveCaseDispute = $"{Base}/disputes/{{disputeId:guid}}/resolve-case";
            public const string RejectDispute = $"{Base}/disputes/{{disputeId:guid}}/reject";
            public const string AddAdminDisputeMessage = $"{Base}/disputes/{{disputeId:guid}}/messages";
            public const string GetDisputeAssignableUsers = $"{Base}/disputes/{{disputeId:guid}}/assignees";

            // Item Moderation
            public const string GetItemReviewQueue = $"{Base}/items/review-queue";
            public const string GetAdminItemDetail = $"{Base}/items/{{itemId:guid}}";
            public const string ApproveItem = $"{Base}/items/{{itemId:guid}}/approve";
            public const string RejectItem = $"{Base}/items/{{itemId:guid}}/reject";
            public const string AssignItemReviewer = $"{Base}/items/{{itemId:guid}}/assign";
            public const string GetItemReviewHistory = $"{Base}/items/{{itemId:guid}}/reviews";
            public const string SetAuctionCuration = $"{Base}/auctions/{{auctionId:guid}}/curation";
            public const string RevealSealedBid = $"{Base}/auctions/{{auctionId:guid}}/sealed-bids/{{sealedBidId:guid}}/reveal";
            public const string TriggerAuctionEmergency = $"{Base}/auctions/{{auctionId:guid}}/emergencies";
            public const string ResolveAuctionEmergency = $"{Base}/auctions/{{auctionId:guid}}/emergencies/{{emergencyId:guid}}/resolve";
            public const string GetReports = $"{Base}/reports";
            public const string AssignReport = $"{Base}/reports/{{reportId:guid}}/assign";
            public const string ResolveReport = $"{Base}/reports/{{reportId:guid}}/resolve";
            public const string EscalateReportEmergency = $"{Base}/reports/{{reportId:guid}}/escalate-emergency";
            public const string EscalateReportToDispute = $"{Base}/reports/{{reportId:guid}}/escalate-to-dispute";
            public const string GetMonitoringAlerts = $"{Base}/monitoring-alerts";
            public const string AcknowledgeMonitoringAlert = $"{Base}/monitoring-alerts/{{alertId:guid}}/acknowledge";
            public const string ResolveMonitoringAlert = $"{Base}/monitoring-alerts/{{alertId:guid}}/resolve";
            public const string FlagUser = $"{Base}/users/{{userId:guid}}/risk-flags";
            public const string FlagAuction = $"{Base}/auctions/{{auctionId:guid}}/alerts";
            public const string CancelInvalidBid = $"{Base}/auctions/{{auctionId:guid}}/bids/{{bidId:guid}}/cancel";

            // Completed Auctions (post-sale monitoring)
            public const string GetCompletedAuctions = $"{Base}/auctions/completed";
            public const string GetCompletedAuctionById = $"{Base}/auctions/completed/{{auctionId:guid}}";

            // Repair utilities (one-off)
            public const string RepairStuckInAuctionItems = $"{Base}/repair/stuck-in-auction-items";

            // Warehouse inspection-reject recovery (Phase D).
            public const string RetryPendingInspectionReject = $"{Base}/warehouse-returns/retry/{{inspectionId:guid}}";
        }

        public static class Auth
        {
            private const string Base = "api/auth";

            public const string Register = $"{Base}/register";
            public const string Login = $"{Base}/login";
            public const string Logout = $"{Base}/logout";
            public const string RefreshToken = $"{Base}/refresh";
            public const string ConfirmEmail = $"{Base}/confirm-email";
            public const string ResendConfirmEmail = $"{Base}/resend-confirm-email";
            public const string ForgotPassword = $"{Base}/forgot-password";
            public const string ResetPassword = $"{Base}/reset-password";
            public const string VerifyTotpLogin = $"{Base}/two-factor/verify";
        }

        public static class Items
        {
            private const string Base = "api/items";

            public const string Create = Base;
            public const string GetAll = Base;
            public const string GetPublic = $"{Base}/public";
            public const string GetById = $"{Base}/{{itemId:guid}}";
            public const string Submit = $"{Base}/{{itemId:guid}}/submit";
            public const string Activate = $"{Base}/{{itemId:guid}}/activate";
            public const string AdminRemove = $"{Base}/{{itemId:guid}}/admin-remove";
            public const string GetBySeller = $"{Base}/my";
            public const string Shipping = $"{Base}/{{itemId:guid}}/shipping";
            public const string CreateAuction = $"{Base}/{{itemId:guid}}/auctions";

            // Images
            public const string AddMedia = $"{Base}/{{itemId:guid}}/media";
            public const string BatchAddMedia = $"{Base}/{{itemId:guid}}/media/batch";
            public const string RemoveMedia = $"{Base}/{{itemId:guid}}/media/{{mediaId:guid}}";
            public const string SetPrimaryImage = $"{Base}/{{itemId:guid}}/media/{{mediaId:guid}}/primary";
            public const string ReorderMedia = $"{Base}/{{itemId:guid}}/media/reorder";

            // Resubmit
            public const string Resubmit = $"{Base}/{{itemId:guid}}/resubmit";
            public const string ConfirmInspectedCondition = $"{Base}/{{itemId:guid}}/confirm-inspected-condition";

            // Questions
            public const string AskQuestion = $"{Base}/{{itemId:guid}}/questions";
            public const string AnswerQuestion = $"{Base}/{{itemId:guid}}/questions/{{questionId:guid}}/answer";
            public const string GetQuestions = $"{Base}/{{itemId:guid}}/questions";
        }

        public static class Auctions
        {
            private const string Base = "api/auctions";

            public const string Create = Base;
            public const string GetAll = Base;
            public const string GetById = $"{Base}/{{auctionId:guid}}";
            public const string Update = $"{Base}/{{auctionId:guid}}";
            public const string GetBids = $"{Base}/{{auctionId:guid}}/bids";
            public const string Cancel = $"{Base}/{{auctionId:guid}}/cancel";
            public const string AdminReject = $"{Base}/{{auctionId:guid}}/admin-reject";
            public const string Close = $"{Base}/{{auctionId:guid}}/close";
            public const string Submit = $"{Base}/{{auctionId:guid}}/submit";
            public const string SetTiming = $"{Base}/{{auctionId:guid}}/timing";
            public const string Shipping = $"{Base}/{{auctionId:guid}}/shipping";
            public const string OfferRunnerUp = $"{Base}/{{auctionId:guid}}/runner-up-offers";
            public const string RespondRunnerUpOffer = $"{Base}/{{auctionId:guid}}/runner-up-offers/respond";
            public const string Relist = $"{Base}/{{auctionId:guid}}/relist";

            // Bidding (REST fallback — primary via SignalR)
            public const string PlaceBid = $"{Base}/{{auctionId:guid}}/bids";
            public const string BuyNow = $"{Base}/{{auctionId:guid}}/buy-now";
            public const string SubmitSealedBid = $"{Base}/{{auctionId:guid}}/sealed-bids";

            // Auto-Bid
            public const string ConfigureAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid";
            public const string PauseAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/pause";
            public const string ResumeAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/resume";
            public const string CancelAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/cancel";
            public const string GetMyAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/my";

            // Watch
            public const string Watch = $"{Base}/{{auctionId:guid}}/watch";
            public const string Unwatch = $"{Base}/{{auctionId:guid}}/watch";
            public const string WatchPreferences = $"{Base}/{{auctionId:guid}}/watch/preferences";
            public const string Deposit = $"{Base}/{{auctionId:guid}}/deposit";
            public const string RecordView = $"{Base}/{{auctionId:guid}}/view";
        }

        public static class Categories
        {
            private const string Base = "api/categories";

            public const string Create = Base;
            public const string GetAll = Base;
            public const string GetById = $"{Base}/{{categoryId:guid}}";
            public const string GetBySlug = $"{Base}/by-slug/{{slug}}";
            public const string GetChildren = $"{Base}/{{categoryId:guid}}/children";
            public const string Update = $"{Base}/{{categoryId:guid}}";
        }
        
        public static class Sellers
        {
            private const string Base = "api/sellers";
            private const string SellerBase = "api/seller";

            public const string GetAll = Base;
            public const string GetById = $"{Base}/{{sellerId:guid}}";
            public const string GetItems = $"{Base}/{{sellerId:guid}}/items";
            public const string GetWalletOverview = $"{SellerBase}/wallet/overview";
        }

        public static class SellerFinance
        {
            private const string Base = "api/seller/finance";

            public const string Overview = $"{Base}/overview";
            public const string EscrowLedger = $"{Base}/escrow-ledger";
        }

        public static class Reviews
        {
            private const string Base = "api/reviews";

            public const string Create = Base;
            public const string GetBySeller = "api/sellers/{sellerId:guid}/reviews";
        }

        public static class Warehouse
        {
            private const string Base = "api/warehouse";
            
            public const string InspectionQueue = $"{Base}/inbound-shipments/inspection-queue";
            public const string InspectWarehouseItem = $"{Base}/inbound-shipments/{{shipmentId:guid}}/inspect";
            public const string ReviewWarehouseInspection = $"{Base}/inbound-shipments/{{shipmentId:guid}}/review";
            public const string BookInbound  = $"{Base}/inbound-shipments";
            public const string BookOutbound = $"{Base}/outbound-shipments";
            public const string WarehouseStaffOutboundQueue = $"{Base}-staff/outbound-orders";
            public const string WarehouseStaffOutboundOrderById = $"{Base}-staff/outbound-orders/{{orderId:guid}}";
            public const string WarehouseStaffOutboundShipments           = $"{Base}-staff/outbound-shipments";
            public const string WarehouseStaffOutboundShipmentById        = $"{Base}-staff/outbound-shipments/{{shipmentId:guid}}";
            public const string WarehouseStaffOutboundShipmentStatus      = $"{Base}-staff/outbound-shipments/{{shipmentId:guid}}/status";
            public const string StoreItem   = $"{Base}/warehouse-items/{{warehouseItemId}}/store";
            public const string GhnWebhook   = "webhooks/ghn";
            public const string StorageLocations = $"{Base}/storage-locations";
            public const string InboundShipmentById  = $"{Base}/inbound-shipments/{{shipmentId:guid}}";
            public const string OutboundShipmentById = $"{Base}/outbound-shipments/{{shipmentId:guid}}";
            public const string WarehouseItems = $"{Base}/warehouse-items";
            public const string WarehouseItemById = $"{Base}/warehouse-items/{{warehouseItemId:guid}}";
            public const string CancelInbound              = $"{Base}/inbound-shipments/{{shipmentId:guid}}/cancel";
            public const string CancelOutbound             = $"{Base}/outbound-shipments/{{shipmentId:guid}}/cancel";
            public const string StorageLocationById        = $"{Base}/storage-locations/{{locationId:guid}}";
            public const string ShippingProviderConfigById = $"{Base}/shipping-provider-configs/{{configId:guid}}";
            public const string SetExternalTracking = $"{Base}/inbound-shipments/{{shipmentId}}/tracking";
            public const string UpdateExternalStatus   = $"{Base}/inbound-shipments/{{shipmentId}}/status";
            public const string QrCode                 = $"{Base}/inbound-shipments/{{shipmentId:guid}}/qr";
            public const string InspectMultipart = $"{Base}/inbound-shipments/{{shipmentId}}/inspect/multipart";
            public const string SelfShipOutbound = $"{Base}/outbound-shipments/self-ship";
            public const string CalculateShippingFee = $"{Base}/shipping/calculate-fee";
            public const string CalculateLeadTime    = $"{Base}/shipping/calculate-lead-time";

            public const string InboundPackages              = $"{Base}/inbound-packages";
            public const string InboundPackageByCode         = $"{Base}/inbound-packages/{{clientOrderCode}}";
            public const string ReceiveInboundPackageMultipart = $"{Base}/inbound-packages/{{clientOrderCode}}/receive/multipart";
            public const string CancelInboundPackage          = $"{Base}/inbound-packages/{{clientOrderCode}}/cancel";
            public const string SetInboundPackageTracking     = $"{Base}/inbound-packages/{{clientOrderCode}}/tracking";
            public const string UpdateInboundPackageStatus    = $"{Base}/inbound-packages/{{clientOrderCode}}/status";

            public const string SellerWarehouseItems     = "api/seller/warehouse/items";
            public const string SellerWarehouseItemById  = "api/seller/warehouse/items/{warehouseItemId:guid}";
            public const string SellerWarehouseRequestReinspection = "api/seller/warehouse/items/{warehouseItemId:guid}/request-reinspection";

            // Buyer-facing outbound shipment QR deep-link (external-carrier flow)
            public const string BuyerOutboundShipmentByToken = "api/buyer/outbound-shipments/by-token";

            // Warehouse → Seller returns (Phase D).
            public const string WarehouseStaffReturns                    = $"{Base}-staff/returns";
            public const string WarehouseStaffReturnMarkShipped          = $"{Base}-staff/returns/{{id:guid}}/ship";
            public const string WarehouseStaffReturnDeliveryFailure      = $"{Base}-staff/returns/{{id:guid}}/delivery-failure";
            public const string SellerWarehouseReturns                   = "api/seller/warehouse-returns";
            public const string SellerWarehouseReturnConfirmReceipt      = "api/seller/warehouse-returns/{id:guid}/confirm-receipt";

            // Warehouse → Seller returns — evidence + QR scan (Phase C).
            public const string WarehouseStaffReturnEvidence             = $"{Base}-staff/returns/{{id:guid}}/evidence";
            public const string SellerWarehouseReturnEvidence            = "api/seller/warehouse-returns/{id:guid}/evidence";
            public const string SellerWarehouseReturnScan                = "api/seller/warehouse-returns/{id:guid}/scan";
        }

        public static class VnPay
        {
            private const string Base = "api/payments/vnpay";

            public const string CreatePaymentUrl = $"{Base}/create-url";
            public const string Return = $"{Base}/return";
            public const string Ipn = $"{Base}/ipn";
            public const string Refund = $"{Base}/refund";
        }

        public static class Address
        {
            private const string Base = "api/address";

            public const string GetProvinces = $"{Base}/provinces";
            public const string GetDistricts = $"{Base}/districts";
            public const string GetWards     = $"{Base}/wards";
            public const string Sync         = $"{Base}/sync";
        }

        public static class Payments
        {
            private const string Base = "api/payments";

            public const string AddMethod = $"{Base}/methods";
            public const string GetMethods = $"{Base}/methods";
            public const string DeleteMethod = $"{Base}/methods/{{id:guid}}";
            public const string HardDeleteMethod = $"{Base}/methods/{{id:guid}}/hard";
            public const string ReactivateMethod = $"{Base}/methods/{{id:guid}}/reactivate";
            public const string CheckoutOrder = $"{Base}/checkout";
            public const string SetDefaultMethod = $"{Base}/methods/{{id:guid}}/default";
            public const string LinkCard = $"{Base}/methods/link-card";
        }

        public static class AdminPayments
        {
            private const string Base = "api/admin/payments";

            public const string GetWithdrawals = $"{Base}/withdrawals";
            public const string GetWithdrawalById = $"{Base}/withdrawals/{{withdrawalId:guid}}";
            public const string ApproveWithdrawal = $"{Base}/withdrawals/{{withdrawalId:guid}}/approve";
            public const string RejectWithdrawal = $"{Base}/withdrawals/{{withdrawalId:guid}}/reject";
            public const string GetTransactions = $"{Base}/transactions";
            public const string GetTransactionById = $"{Base}/transactions/{{transactionId:guid}}";
            public const string GetEscrows = $"{Base}/escrows";
            public const string GetEscrowById = $"{Base}/escrows/{{escrowId:guid}}";
            public const string GetSummary = $"{Base}/summary";
            public const string GetPlatformWallet = $"{Base}/platform-wallet";
            public const string CompleteWithdrawal = $"{Base}/withdrawals/{{withdrawalId:guid}}/complete";
        }

        public static class Orders
        {
            private const string Base = "api/orders";

            public const string GetById = $"{Base}/{{orderId:guid}}";
            public const string CreateReturn = $"{Base}/{{orderId:guid}}/returns";
            public const string ShipReturn = $"{Base}/{{orderId:guid}}/returns/{{returnId:guid}}/ship";
            public const string ApproveReturn = $"{Base}/{{orderId:guid}}/returns/{{returnId:guid}}/approve";
            public const string RejectReturn = $"{Base}/{{orderId:guid}}/returns/{{returnId:guid}}/reject";
            public const string ConfirmReturnReceived = $"{Base}/{{orderId:guid}}/returns/{{returnId:guid}}/confirm-received";

            // Return flows — evidence + QR scan (Phase C).
            public const string AddBuyerReturnEvidence = $"api/me/orders/{{orderId:guid}}/returns/{{returnId:guid}}/evidence";
            public const string AddSellerReturnEvidence = $"api/seller/orders/{{orderId:guid}}/returns/{{returnId:guid}}/evidence";
            public const string ScanReturn = $"api/seller/orders/{{orderId:guid}}/returns/{{returnId:guid}}/scan";
            public const string RetryDeferredRefund = $"api/admin/orders/{{orderId:guid}}/returns/{{returnId:guid}}/retry-refund";

            public const string ConfirmReceipt = $"{Base}/{{orderId:guid}}/confirm-receipt";
            public const string UpdateShipping = $"{Base}/{{orderId:guid}}/shipping";
            public const string Confirm = $"{Base}/{{orderId:guid}}/confirm";
            public const string MarkPickedUp = $"{Base}/{{orderId:guid}}/mark-picked-up";
            public const string MarkOnDelivering = $"{Base}/{{orderId:guid}}/mark-on-delivering";
            public const string MarkDelivered = $"{Base}/{{orderId:guid}}/mark-delivered";

            // Seller direct shipments (1:1 with order)
            public const string CreateSellerDirectShipment = $"{Base}/{{orderId:guid}}/self-shipments";
            public const string SetSellerDirectShipmentCarrierInfo = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/carrier-info";
            public const string MarkSellerDirectShipmentPickedUp = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/mark-picked-up";
            public const string MarkSellerDirectShipmentOnDelivering = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/mark-on-delivering";
            public const string MarkSellerDirectShipmentDelivered = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/mark-delivered";
            public const string SetSellerDirectShipmentDispatchDetails = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/dispatch-details";
            public const string AddSellerDirectShipmentHandoverProofs = $"{Base}/{{orderId:guid}}/self-shipments/{{shipmentId:guid}}/handover-proofs";
        }

        public static class Me
        {
            private const string Base = "api/me";

            public const string MyAuctions = $"{Base}/auctions";
            public const string MyBids = $"{Base}/bids";
            public const string MyAuctionWatchlist = $"{Base}/auctions/watch-list";
            public const string MyPendingWinnerOffers = $"{Base}/winner-offers";
            public const string AddAddress = $"{Base}/addresses";
            public const string ChangePassword = $"{Base}/password";
            public const string ConfirmPhoneNumber = $"{Base}/phone/confirm";
            public const string DisableTwoFactor = $"{Base}/two-factor/disable";
            public const string EnableTwoFactor = $"{Base}/two-factor/enable";
            public const string SetupTotp = $"{Base}/two-factor/setup";
            public const string ConfirmTotpSetup = $"{Base}/two-factor/confirm";
            public const string RegenerateRecoveryCodes = $"{Base}/two-factor/recovery-codes";
            public const string GetActiveSessions = $"{Base}/sessions";
            public const string GetAddresses = $"{Base}/addresses";
            public const string GetCurrentUser = $"{Base}";
            public const string GetCurrentUserProfile = $"{Base}/profile";
            public const string GetLoginHistory = $"{Base}/login-history";
            public const string RemoveMyAddress = $"{Base}/addresses/{{addressId:guid}}";
            public const string SetDefaultAddress = $"{Base}/addresses/{{addressId:guid}}/default";
            public const string SetPhoneNumber = $"{Base}/phone";
            public const string UpdateAddress = $"{Base}/addresses/{{addressId:guid}}";
            public const string UpdateCurrentUserProfile = $"{Base}/profile";
            public const string AcceptTerm = $"{Base}/terms/{{termDocumentId:guid}}/accept";
            public const string GetMyAcceptedTerms = $"{Base}/terms";
            public const string CheckPendingTerms = $"{Base}/terms/pending";
            public const string GetMyWallet = $"{Base}/wallet";
            public const string GetMyWalletTransactions = $"{Base}/wallet/transactions";
            public const string GetMyWalletTransactionById = $"{Base}/wallet/transactions/{{transactionId:guid}}";
            public const string CreateWithdrawal = $"{Base}/wallet/withdrawals";
            public const string GetMyWithdrawals = $"{Base}/wallet/withdrawals";
            public const string CancelWithdrawal = $"{Base}/wallet/withdrawals/{{withdrawalId:guid}}/cancel";
            public const string GetMyOrders = $"{Base}/orders";
            public const string GetMyOutboundShipments = $"{Base}/outbound-shipments";
            public const string AcknowledgeOutboundShipmentReceived = $"{Base}/outbound-shipments/{{shipmentId:guid}}/acknowledge-received";
            public const string GetBuyerOutboundShipmentById = $"{Base}/outbound-shipments/{{shipmentId:guid}}";
            public const string SubmitOutboundShipmentReceiptProof = $"{Base}/outbound-shipments/{{shipmentId:guid}}/proof-of-receipt";
            public const string GetMyInboundShipments = $"{Base}/inbound-shipments";
            public const string GetSellerDirectShipOrders = $"{Base}/orders/seller-direct-ship";
            public const string GetSellerOutboundShipments = $"{Base}/orders/seller-direct-ship/outbound-shipments";
            public const string GetSellerOutboundShipmentById = $"{Base}/orders/seller-direct-ship/outbound-shipments/{{shipmentId:guid}}";
            public const string GetSellerShippingProviderOptions = $"{Base}/orders/seller-direct-ship/shipping-provider-options";
            public const string GetSellerDirectShipmentById = $"{Base}/orders/seller-direct-ship/shipments/{{shipmentId:guid}}";
            public const string GetSellerDirectShipments = $"{Base}/orders/seller-direct-ship/shipments";

            // Unified buyer shipments feed (seller-direct + warehouse-outbound).
            public const string GetMyShipments = $"{Base}/shipments";

            // Seller direct shipments (buyer-facing deep link). Legacy list
            // endpoint now lives at /me/direct-shipments so the unified feed
            // can own /me/shipments. Per-shipment routes keep their original
            // paths since they're keyed by id and never collide.
            public const string GetMyDirectShipments = $"{Base}/direct-shipments";
            public const string GetMyDirectShipmentById = $"{Base}/shipments/{{shipmentId:guid}}";
            public const string AcknowledgeDirectShipmentReceived = $"{Base}/shipments/{{shipmentId:guid}}/acknowledge-received";
            public const string SubmitProofOfDelivery = $"{Base}/shipments/{{shipmentId:guid}}/proof-of-delivery";
            public const string ValidateDirectShipmentScan = $"{Base}/shipments/scan/validate";

            // Verifications
            public const string CreateVerification = $"{Base}/verifications";
            public const string GetMyVerifications = $"{Base}/verifications";
            public const string GetMyVerificationById = $"{Base}/verifications/{{verificationId:guid}}";
            public const string UpdateVerification = $"{Base}/verifications/{{verificationId:guid}}";
            public const string UploadVerificationDocument = $"{Base}/verifications/{{verificationId:guid}}/documents";
            public const string DeleteVerificationDocument = $"{Base}/verifications/{{verificationId:guid}}/documents/{{docId:guid}}";
            public const string SubmitVerification = $"{Base}/verifications/{{verificationId:guid}}/submit";
            public const string CreateVerificationDispute = $"{Base}/verifications/{{verificationId:guid}}/disputes";

            // Seller Profile
            public const string CreateSellerProfile = $"{Base}/seller-profile";
            public const string GetMySellerProfile = $"{Base}/seller-profile";
            public const string UpdateSellerProfile = $"{Base}/seller-profile";

            // Notification Preferences
            public const string NotificationPreferences = $"{Base}/notification-preferences";

            // Disputes
            public const string GetMyDisputes = $"{Base}/disputes";
            public const string GetMyDisputeById = $"{Base}/disputes/{{disputeId:guid}}";
            public const string AddBuyerDisputeMessage = $"{Base}/disputes/{{disputeId:guid}}/messages";
            public const string AddBuyerDisputeEvidence = $"{Base}/disputes/{{disputeId:guid}}/evidence";
        }

        public static class Reports
        {
            private const string Base = "api/reports";

            public const string Create = Base;
            public const string GetMine = "api/me/reports";
        }

        public static class Disputes
        {
            private const string Base = "api/disputes";

            public const string GetMine = Base;
            public const string GetById = $"{Base}/{{disputeId:guid}}";
            public const string GetMessages = $"{Base}/{{disputeId:guid}}/messages";
            public const string SendMessage = $"{Base}/{{disputeId:guid}}/messages";
            public const string MarkRead = $"{Base}/{{disputeId:guid}}/read";
            public const string Eligibility = $"{Base}/eligibility";

            // Intake endpoints
            public const string CreateOrderDispute = "api/orders/{orderId:guid}/disputes";
            public const string CreateAuctionDispute = "api/auctions/{auctionId:guid}/disputes";
            public const string CreatePaymentDispute = "api/payments/{paymentId:guid}/disputes";
            public const string CreateWarehouseItemDispute = "api/warehouse/items/{warehouseItemId:guid}/disputes";
            public const string CreateShipmentDispute = "api/shipments/{shipmentId:guid}/disputes";
        }

        public static class Notifications
        {
            private const string Base = "api/notifications";

            public const string GetMyNotifications = Base;
            public const string GetUnreadCount = $"{Base}/unread-count";
            public const string MarkAsRead = $"{Base}/{{notificationId:guid}}/read";
            public const string MarkAllAsRead = $"{Base}/read-all";
        }

        public static class System
        {
            private const string Base = "api/system";

            public const string GetTime = $"{Base}/time";
        }
    }
}
