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
            public const string AddAddress = nameof(AddAddress);
            public const string ChangePassword = nameof(ChangePassword);
            public const string ConfirmPhoneNumber = nameof(ConfirmPhoneNumber);
            public const string DisableTwoFactor = nameof(DisableTwoFactor);
            public const string EnableTwoFactor = nameof(EnableTwoFactor);
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

            // Verifications
            public const string CreateVerification = nameof(CreateVerification);
            public const string GetMyVerifications = nameof(GetMyVerifications);
            public const string GetMyVerificationById = nameof(GetMyVerificationById);
            public const string UpdateVerification = nameof(UpdateVerification);
            public const string UploadVerificationDocument = nameof(UploadVerificationDocument);
            public const string DeleteVerificationDocument = nameof(DeleteVerificationDocument);
            public const string SubmitVerification = nameof(SubmitVerification);

            // Seller Profile
            public const string CreateSellerProfile = nameof(CreateSellerProfile);
            public const string GetMySellerProfile = nameof(GetMySellerProfile);
            public const string UpdateSellerProfile = nameof(UpdateSellerProfile);
        }

        public static class Media
        {
            public const string RequestUploadSignature = nameof(RequestUploadSignature);
            public const string ConfirmUpload = nameof(ConfirmUpload);
            public const string GetUploadContexts = nameof(GetUploadContexts);
        }

        public static class Admins
        {
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
            public const string GetAllSettings = nameof(GetAllSettings);
            public const string GetSettingByKey = nameof(GetSettingByKey);
            public const string UpdateSetting = nameof(UpdateSetting);
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
        }

        public static class Items
        {
            public const string CreateItem = nameof(CreateItem);
            public const string GetAllItems = nameof(GetAllItems);
            public const string GetItemById = nameof(GetItemById);
            public const string GetMyItems = nameof(GetMyItems);
            public const string ActivateItem = nameof(ActivateItem);
            public const string AddItemMedia = nameof(AddItemMedia);
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
            public const string GetAuctionBids = nameof(GetAuctionBids);
            public const string CancelAuction = nameof(CancelAuction);
            public const string PublishAuction = nameof(PublishAuction);
            public const string PlaceBid = nameof(PlaceBid);
            public const string BuyNow = nameof(BuyNow);
            public const string ConfigureAutoBid = nameof(ConfigureAutoBid);
            public const string PauseAutoBid = nameof(PauseAutoBid);
            public const string ResumeAutoBid = nameof(ResumeAutoBid);
            public const string GetMyAutoBid = nameof(GetMyAutoBid);
            public const string WatchAuction = nameof(WatchAuction);
            public const string UnwatchAuction = nameof(UnwatchAuction);
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
            public const string GetSellerById = nameof(GetSellerById);
        }

        public static class Warehouse
        {
            public const string BookInboundShipment  = nameof(BookInboundShipment);
            public const string BookOutboundShipment = nameof(BookOutboundShipment);
            public const string GhnWebhook           = nameof(GhnWebhook);
            public const string InspectWarehouseItem = nameof(InspectWarehouseItem);
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
        }
    }
}