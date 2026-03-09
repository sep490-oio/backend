namespace OIO.Api.Common;



public static class ApiEndpoint
{
    public static class Tags
    {
        public const string Auth = nameof(Auth);
        public const string Admins =  nameof(Admins);
        public const string Items = nameof(Items);
        public const string Auctions = nameof(Auctions);
        public const string Categories = nameof(Categories);
        public const string Media = nameof(Media);
        public const string Me = nameof(Me);
        public const string Warehouse = nameof(Warehouse);
        public const string Webhooks  = nameof(Webhooks);
    }

    public static class Names
    {
        
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
        }

        public static class Auth
        {
            public const string ConfirmEmail = nameof(ConfirmEmail);
            public const string Login = nameof(Login);
            public const string Logout =  nameof(Logout);
            public const string RefreshToken = nameof(RefreshToken);
            public const string Register = nameof(Register);
            public const string ResendConfirmEmail = nameof(ResendConfirmEmail);
            public const string ForgotPassword = nameof(ForgotPassword);
            public const string ResetPassword = nameof(ResetPassword);

        }
        
        public static class Items
        {
            public const string CreateItem = nameof(CreateItem);
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

    public static class Url
    {
        public static class Media
        {
            private const string Base = "api/media";

            public const string RequestSignature = $"{Base}/upload-signature";
            public const string Confirm = $"{Base}/confirm";
            public const string Contexts = $"{Base}/contexts";
        }
        
        public static class Admins
        {
            private const string Base = "api/admin";
            
            public const string GetUsers = $"{Base}/users";
            public const string GetRoles = $"{Base}/roles";
            public const string GetPermissions = $"{Base}/permissions";
            public const string GetUser = $"{Base}/users/{{userId:guid}}";
            public const string GetAllSettings = $"{Base}/settings";
            public const string GetSettingByKey = $"{Base}/settings/{{key}}";
            public const string UpdateSetting = $"{Base}/settings/{{key}}";
            
            public const string RevokeRole = $"{Base}/users/{{userId:guid}}/roles/{{roleId:int}}";
            public const string RevokePermission = $"{Base}/users/{{userId:guid}}/permissions/{{permissionId:int}}";
            public const string RemoveUser = $"{Base}/users/{{userId:guid}}";
            
            public const string AssignRole = $"{Base}/users/{{userId:guid}}/roles/{{roleId:int}}";
            public const string ChangeUserStatus = $"{Base}/users/{{userId:guid}}/status";
            public const string GrantPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permissionId:int}}";
            public const string DenyPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permissionId:int}}";
            public const string UnlockUser = $"{Base}/users/{{userId:guid}}/unlock";
            public const string TogglePermission = $"{Base}/roles/{{roleId:int}}/permissions/{{permissionId:int}}";
        }

        public static class Auth
        {
            private const string Base = "api/auth";
            
            public const string Register = $"{Base}/register";
            public const string Login = $"{Base}/login";
            public const string Logout =  $"{Base}/logout";
            public const string RefreshToken = $"{Base}/refresh";
            public const string ConfirmEmail = $"{Base}/confirm-email";
            public const string ResendConfirmEmail = $"{Base}/resend-confirm-email";
            public const string ForgotPassword = $"{Base}/forgot-password";
            public const string ResetPassword = $"{Base}/reset-password";

        }
        
        public static class Items
        {
            private const string Base = "api/items";

            public const string Create = Base;
            public const string GetById = $"{Base}/{{itemId:guid}}";
            public const string Activate = $"{Base}/{{itemId:guid}}/activate";
            public const string GetBySeller = $"{Base}/my";

            // Images
            public const string AddMedia = $"{Base}/{{itemId:guid}}/media";
            public const string RemoveMedia = $"{Base}/{{itemId:guid}}/media/{{mediaId:guid}}";
            public const string SetPrimaryImage =  $"{Base}/{{itemId:guid}}/media/{{mediaId:guid}}/primary";
            public const string ReorderMedia = $"{Base}/{{itemId:guid}}/media/reorder";
            
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
            public const string GetBids = $"{Base}/{{auctionId:guid}}/bids";
            public const string Cancel = $"{Base}/{{auctionId:guid}}/cancel";
            public const string Publish = $"{Base}/{{auctionId:guid}}/publish";

            // Bidding (REST fallback — primary via SignalR)
            public const string PlaceBid = $"{Base}/{{auctionId:guid}}/bids";
            public const string BuyNow = $"{Base}/{{auctionId:guid}}/buy-now";

            // Auto-Bid
            public const string ConfigureAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid";
            public const string PauseAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/pause";
            public const string ResumeAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/resume";
            public const string GetMyAutoBid = $"{Base}/{{auctionId:guid}}/auto-bid/my";

            // Watch
            public const string Watch = $"{Base}/{{auctionId:guid}}/watch";
            public const string Unwatch = $"{Base}/{{auctionId:guid}}/watch";
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
        public static class Warehouse
        {
            private const string Base = "api/warehouse";
            
            public const string InspectWarehouseItem = $"{Base}/inbound-shipments/{{shipmentId}}/inspect";
            public const string BookInbound  = $"{Base}/inbound-shipments";
            public const string BookOutbound = $"{Base}/outbound-shipments";
            public const string StoreItem   = $"{Base}/warehouse-items/{{warehouseItemId}}/store";
            public const string GhnWebhook   = "webhooks/ghn";
            public const string StorageLocations = $"{Base}/storage-locations";
            public const string InboundShipmentById  = $"{Base}/inbound-shipments/{{shipmentId:guid}}";
            public const string OutboundShipmentById = $"{Base}/outbound-shipments/{{shipmentId:guid}}";
            public const string WarehouseItems = $"{Base}/warehouse-items";
            public const string CancelInbound              = $"{Base}/inbound-shipments/{{shipmentId:guid}}/cancel";
            public const string CancelOutbound             = $"{Base}/outbound-shipments/{{shipmentId:guid}}/cancel";
            public const string StorageLocationById        = $"{Base}/storage-locations/{{locationId:guid}}";
            public const string ShippingProviderConfigById = $"{Base}/shipping-provider-configs/{{configId:guid}}";
        }
        public static class Me
        {
            private const string Base = "api/me";

            public const string MyAuctions = $"{Base}/auctions";
            public const string MyBids = $"{Base}/bids";
            public const string MyAuctionWatchlist = $"{Base}/auctions/watch-list";
            public const string AddAddress = $"{Base}/addresses";
            public const string ChangePassword = $"{Base}/password";
            public const string ConfirmPhoneNumber = $"{Base}/phone/confirm";
            public const string DisableTwoFactor = $"{Base}/two-factor/disable";
            public const string EnableTwoFactor = $"{Base}/two-factor/enable";
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
        }

    }
    


}