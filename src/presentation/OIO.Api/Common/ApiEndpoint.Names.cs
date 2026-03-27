namespace OIO.Api.Common;

public static partial class ApiEndpoint
{
    public static class Names
    {
        public static class Terms
        {
            public const string GetActiveTerms = nameof(GetActiveTerms);
            public const string GetActiveTermsByType = nameof(GetActiveTermsByType);
            public const string GetTermById = nameof(GetTermById);
        }
        public static class Me
        {
            public const string GetMyAuctions = nameof(GetMyAuctions);
            public const string GetMyBids = nameof(GetMyBids);
            public const string GetMyAuctionWatchlist = nameof(GetMyAuctionWatchlist);
            public const string GetMyPendingWinnerOffers = nameof(GetMyPendingWinnerOffers);
            public const string AddAddress = nameof(AddAddress);
            public const string ChangePassword = nameof(ChangePassword);
            public const string ConfirmPhoneNumber = nameof(ConfirmPhoneNumber);
            public const string DisableTwoFactor = nameof(DisableTwoFactor);
            public const string EnableTwoFactor = nameof(EnableTwoFactor);
            public const string SetupTotp = nameof(SetupTotp);
            public const string ConfirmTotpSetup = nameof(ConfirmTotpSetup);
            public const string RegenerateRecoveryCodes = nameof(RegenerateRecoveryCodes);
            public const string GetActiveSessions = nameof(GetActiveSessions);
            public const string GetAddresses = nameof(GetAddresses);
            public const string GetMe = nameof(GetMe);
            public const string GetMyProfile = nameof(GetMyProfile);
            public const string GetMyLoginHistory = nameof(GetMyLoginHistory);
            public const string RemoveMyAddress = nameof(RemoveMyAddress);
            public const string SetDefaultAddress = nameof(SetDefaultAddress);
            public const string SetPhoneNumber = nameof(SetPhoneNumber);
            public const string UpdateMyAddress = nameof(UpdateMyAddress);
            public const string UpdateMyProfile = nameof(UpdateMyProfile);
            public const string AcceptTerm = nameof(AcceptTerm);
            public const string GetMyAcceptedTerms = nameof(GetMyAcceptedTerms);
            public const string CheckPendingTerms = nameof(CheckPendingTerms);
            public const string GetMyWallet = nameof(GetMyWallet);
            public const string GetMyOrders = nameof(GetMyOrders);

            // Verifications
            public const string CreateVerification = nameof(CreateVerification);
            public const string GetMyVerifications = nameof(GetMyVerifications);
            public const string GetMyVerificationById = nameof(GetMyVerificationById);
            public const string UpdateVerification = nameof(UpdateVerification);
            public const string UploadVerificationDocument = nameof(UploadVerificationDocument);
            public const string DeleteVerificationDocument = nameof(DeleteVerificationDocument);
            public const string SubmitVerification = nameof(SubmitVerification);
            public const string CreateVerificationDispute = nameof(CreateVerificationDispute);

            // Seller Profile
            public const string CreateSellerProfile = nameof(CreateSellerProfile);
            public const string GetMySellerProfile = nameof(GetMySellerProfile);
            public const string UpdateSellerProfile = nameof(UpdateSellerProfile);

            // Notification Preferences
            public const string GetNotificationPreferences = nameof(GetNotificationPreferences);
            public const string UpdateNotificationPreferences = nameof(UpdateNotificationPreferences);
        }

        public static class Media
        {
            public const string RequestUploadSignature = nameof(RequestUploadSignature);
            public const string ConfirmUpload = nameof(ConfirmUpload);
            public const string GetUploadContexts = nameof(GetUploadContexts);
            public const string BatchRequestUploadSignature = nameof(BatchRequestUploadSignature);
            public const string BatchConfirmUpload = nameof(BatchConfirmUpload);
        }

        public static class Admins
        {
            public const string CreateUser = nameof(CreateUser);
            public const string GetUserById = nameof(GetUserById);
            public const string GetUsers = nameof(GetUsers);
            public const string GetRoles = nameof(GetRoles);
            public const string GetPermissions = nameof(GetPermissions);
            public const string ChangeUserStatus = nameof(ChangeUserStatus);
            public const string AssignRole = nameof(AssignRole);
            public const string RevokeRole = nameof(RevokeRole);
            public const string RemoveUser = nameof(RemoveUser);
            public const string GrantPermission = nameof(GrantPermission);
            public const string DenyPermission = nameof(DenyPermission);
            public const string RevokePermission = nameof(RevokePermission);
            public const string TogglePermission = nameof(TogglePermission);
            public const string UnlockUser = nameof(UnlockUser);
            public const string CreateTermsDocument = nameof(CreateTermsDocument);
            public const string ActivateTermsDocument = nameof(ActivateTermsDocument);
            public const string GetAllTermsDocuments = nameof(GetAllTermsDocuments);

            // Verifications
            public const string GetPendingVerifications = nameof(GetPendingVerifications);
            public const string GetVerificationById = nameof(GetVerificationById);
            public const string ApproveVerification = nameof(ApproveVerification);
            public const string RejectVerification = nameof(RejectVerification);

            // Seller Profiles
            public const string GetSellerProfiles = nameof(GetSellerProfiles);
            public const string VerifySellerProfile = nameof(VerifySellerProfile);
            public const string RejectSellerProfile = nameof(RejectSellerProfile);

            // Disputes
            public const string ResolveDispute = nameof(ResolveDispute);

            // Item Moderation
            public const string GetItemReviewQueue = nameof(GetItemReviewQueue);
            public const string GetAdminItemDetail = nameof(GetAdminItemDetail);
            public const string ApproveItem = nameof(ApproveItem);
            public const string RejectItem = nameof(RejectItem);
            public const string AssignItemReviewer = nameof(AssignItemReviewer);
            public const string GetItemReviewHistory = nameof(GetItemReviewHistory);
            public const string SetAuctionCuration = nameof(SetAuctionCuration);
            public const string RevealSealedBid = nameof(RevealSealedBid);
            public const string TriggerAuctionEmergency = nameof(TriggerAuctionEmergency);
            public const string ResolveAuctionEmergency = nameof(ResolveAuctionEmergency);
            public const string GetReports = nameof(GetReports);
            public const string AssignReport = nameof(AssignReport);
            public const string ResolveReport = nameof(ResolveReport);
            public const string EscalateReportEmergency = nameof(EscalateReportEmergency);
            public const string GetMonitoringAlerts = nameof(GetMonitoringAlerts);
            public const string AcknowledgeMonitoringAlert = nameof(AcknowledgeMonitoringAlert);
            public const string ResolveMonitoringAlert = nameof(ResolveMonitoringAlert);
            public const string FlagUser = nameof(FlagUser);
            public const string FlagAuction = nameof(FlagAuction);
            public const string CancelInvalidBid = nameof(CancelInvalidBid);
        }

        public static class Auth
        {
            public const string ConfirmEmail = nameof(ConfirmEmail);
            public const string Login = nameof(Login);
            public const string Logout = nameof(Logout);
            public const string RefreshToken = nameof(RefreshToken);
            public const string Register = nameof(Register);
            public const string ResendConfirmEmail = nameof(ResendConfirmEmail);
            public const string ForgotPassword = nameof(ForgotPassword);
            public const string ResetPassword = nameof(ResetPassword);
            public const string VerifyTotpLogin = nameof(VerifyTotpLogin);
        }

        public static class Items
        {
            public const string CreateItem = nameof(CreateItem);
            public const string GetAllItems = nameof(GetAllItems);
            public const string GetPublicItems = nameof(GetPublicItems);
            public const string GetItemById = nameof(GetItemById);
            public const string GetMyItems = nameof(GetMyItems);
            public const string SubmitItem = nameof(SubmitItem);
            public const string ActivateItem = nameof(ActivateItem);
            public const string ResubmitItem = nameof(ResubmitItem);
            public const string ConfirmInspectedCondition = nameof(ConfirmInspectedCondition);
            public const string ChooseItemShipping = nameof(ChooseItemShipping);
            public const string CreateAuctionFromItem = nameof(CreateAuctionFromItem);
            public const string AddItemMedia = nameof(AddItemMedia);
            public const string BatchAddItemMedia = nameof(BatchAddItemMedia);
            public const string RemoveItemMedia = nameof(RemoveItemMedia);
            public const string SetPrimaryItemImage = nameof(SetPrimaryItemImage);
            public const string ReorderItemMedia = nameof(ReorderItemMedia);
            public const string AskItemQuestion = nameof(AskItemQuestion);
            public const string AnswerItemQuestion = nameof(AnswerItemQuestion);
            public const string GetItemQuestions = nameof(GetItemQuestions);
        }

        public static class Auctions
        {
            public const string GetAllAuctions = nameof(GetAllAuctions);
            public const string CreateAuction = nameof(CreateAuction);
            public const string GetAuctionById = nameof(GetAuctionById);
            public const string UpdateAuction = nameof(UpdateAuction);
            public const string GetAuctionBids = nameof(GetAuctionBids);
            public const string CancelAuction = nameof(CancelAuction);
            public const string CloseAuction = nameof(CloseAuction);
            public const string PublishAuction = nameof(PublishAuction);
            public const string SubmitAuction = nameof(SubmitAuction);
            public const string SetAuctionTiming = nameof(SetAuctionTiming);
            public const string ChooseAuctionShipping = nameof(ChooseAuctionShipping);
            public const string OfferRunnerUp = nameof(OfferRunnerUp);
            public const string RespondRunnerUpOffer = nameof(RespondRunnerUpOffer);
            public const string RelistAuction = nameof(RelistAuction);
            public const string PlaceBid = nameof(PlaceBid);
            public const string BuyNow = nameof(BuyNow);
            public const string SubmitSealedBid = nameof(SubmitSealedBid);
            public const string ConfigureAutoBid = nameof(ConfigureAutoBid);
            public const string PauseAutoBid = nameof(PauseAutoBid);
            public const string ResumeAutoBid = nameof(ResumeAutoBid);
            public const string GetMyAutoBid = nameof(GetMyAutoBid);
            public const string WatchAuction = nameof(WatchAuction);
            public const string UnwatchAuction = nameof(UnwatchAuction);
            public const string UpdateWatcherPreferences = nameof(UpdateWatcherPreferences);
            public const string RecordAuctionView = nameof(RecordAuctionView);
            public const string DepositFromWallet = nameof(DepositFromWallet);
        }

        public static class Categories
        {
            public const string CreateCategory = nameof(CreateCategory);
            public const string GetAllCategories = nameof(GetAllCategories);
            public const string GetCategoryById = nameof(GetCategoryById);
            public const string GetCategoryBySlug = nameof(GetCategoryBySlug);
            public const string GetCategoryChildren = nameof(GetCategoryChildren);
            public const string UpdateCategory = nameof(UpdateCategory);
        }
        
        public static class Sellers
        {
            public const string GetSellers = nameof(GetSellers);
            public const string GetSellerById = nameof(GetSellerById);
            public const string GetSellerItems = nameof(GetSellerItems);
        }

        public static class Reviews
        {
            public const string CreateSellerReview = nameof(CreateSellerReview);
            public const string GetSellerReviews = nameof(GetSellerReviews);
        }

        public static class Warehouse
        {
            public const string BookInboundShipment  = nameof(BookInboundShipment);
            public const string BookOutboundShipment = nameof(BookOutboundShipment);
            public const string GhnWebhook           = nameof(GhnWebhook);
            public const string GetInspectionQueue   = nameof(GetInspectionQueue);
            public const string InspectWarehouseItem = nameof(InspectWarehouseItem);
            public const string ReviewWarehouseInspection = nameof(ReviewWarehouseInspection);
            public const string StoreWarehouseItem   = nameof(StoreWarehouseItem);
            public const string CreateStorageLocation = nameof(CreateStorageLocation);
            public const string GetStorageLocations   = nameof(GetStorageLocations);
            public const string GetInboundShipments      = nameof(GetInboundShipments);
            public const string GetInboundShipmentById   = nameof(GetInboundShipmentById);
            public const string GetOutboundShipments     = nameof(GetOutboundShipments);
            public const string GetOutboundShipmentById  = nameof(GetOutboundShipmentById);
            public const string GetWarehouseItems = nameof(GetWarehouseItems);
            public const string CancelInboundShipment          = nameof(CancelInboundShipment);
            public const string CancelOutboundShipment         = nameof(CancelOutboundShipment);
            public const string DeleteStorageLocation          = nameof(DeleteStorageLocation);
            public const string UpdateStorageLocation          = nameof(UpdateStorageLocation);
            public const string UpdateShippingProviderConfig   = nameof(UpdateShippingProviderConfig);
            public const string SetExternalTrackingNumber      = nameof(SetExternalTrackingNumber);
            public const string UpdateExternalShipmentStatus   = nameof(UpdateExternalShipmentStatus);
            public const string GetInboundShipmentQrCode       = nameof(GetInboundShipmentQrCode);
            public const string InspectWarehouseItemMultipart = nameof(InspectWarehouseItemMultipart);
            public const string SelfShipOrder = nameof(SelfShipOrder);
        }

        public static class VnPay
        {
            public const string CreatePaymentUrl = nameof(CreatePaymentUrl);
            public const string Return = "VnPayReturn";
            public const string Ipn = "VnPayIpn";
            public const string Refund = "VnPayRefund";
        }

        public static class Payments
        {
            public const string AddPaymentMethod = nameof(AddPaymentMethod);
            public const string GetPaymentMethods = nameof(GetPaymentMethods);
            public const string DeletePaymentMethod = nameof(DeletePaymentMethod);
            public const string CheckoutOrder =  nameof(CheckoutOrder);
            public const string SetDefaultPaymentMethod = nameof(SetDefaultPaymentMethod);
            public const string LinkCardViaVnPay = nameof(LinkCardViaVnPay);
        }

        public static class AdminPayments
        {
            public const string GetWithdrawals = nameof(GetWithdrawals);
            public const string GetWithdrawalById = nameof(GetWithdrawalById);
            public const string ApproveWithdrawal = nameof(ApproveWithdrawal);
            public const string RejectWithdrawal = nameof(RejectWithdrawal);
            public const string GetTransactions = nameof(GetTransactions);
            public const string GetTransactionById = nameof(GetTransactionById);
            public const string GetEscrows = nameof(GetEscrows);
            public const string GetEscrowById = nameof(GetEscrowById);
            public const string GetSummary = nameof(GetSummary);
            public const string GetPlatformWallet = nameof(GetPlatformWallet);
            public const string CompleteWithdrawal = nameof(CompleteWithdrawal);
        }

        public static class Orders
        {
            public const string GetOrderById = nameof(GetOrderById);
            public const string CreateOrderReturn = nameof(CreateOrderReturn);
            public const string ShipOrderReturn = nameof(ShipOrderReturn);
            public const string ApproveOrderReturn = nameof(ApproveOrderReturn);
            public const string RejectOrderReturn = nameof(RejectOrderReturn);
            public const string ConfirmOrderReturnReceived = nameof(ConfirmOrderReturnReceived);
        }

        public static class Reports
        {
            public const string CreateReport = nameof(CreateReport);
            public const string GetMyReports = nameof(GetMyReports);
        }

        public static class Disputes
        {
            public const string GetAccessibleDisputes = nameof(GetAccessibleDisputes);
            public const string GetDisputeThread = nameof(GetDisputeThread);
            public const string GetDisputeMessages = nameof(GetDisputeMessages);
            public const string SendDisputeMessage = nameof(SendDisputeMessage);
            public const string MarkDisputeRead = nameof(MarkDisputeRead);
        }

        public static class Notifications
        {
            public const string GetMyNotifications = nameof(GetMyNotifications);
            public const string GetUnreadCount = nameof(GetUnreadCount);
            public const string MarkAsRead = nameof(MarkAsRead);
            public const string MarkAllAsRead = nameof(MarkAllAsRead);
        }

        public static class Wallet
        {
            public const string GetMyWallet = nameof(GetMyWallet);
            public const string GetMyWalletTransactions = nameof(GetMyWalletTransactions);
            public const string GetMyWalletTransactionById = nameof(GetMyWalletTransactionById);
            public const string CreateWithdrawal = nameof(CreateWithdrawal);
            public const string GetMyWithdrawals = nameof(GetMyWithdrawals);
            public const string CancelWithdrawal = nameof(CancelWithdrawal);
        }
    }
}
