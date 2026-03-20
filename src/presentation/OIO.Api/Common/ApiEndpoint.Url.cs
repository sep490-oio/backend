namespace OIO.Api.Common;

public static partial class ApiEndpoint
{
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

            public const string RevokeRole = $"{Base}/users/{{userId:guid}}/roles/{{role}}";
            public const string RevokePermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string RemoveUser = $"{Base}/users/{{userId:guid}}";

            public const string AssignRole = $"{Base}/users/{{userId:guid}}/roles/{{role}}";
            public const string ChangeUserStatus = $"{Base}/users/{{userId:guid}}/status";
            public const string GrantPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string DenyPermission = $"{Base}/users/{{userId:guid}}/permissions/{{permission}}";
            public const string UnlockUser = $"{Base}/users/{{userId:guid}}/unlock";
            public const string TogglePermission = $"{Base}/roles/{{role}}/permissions/{{permission}}";
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
            public const string SetPrimaryImage = $"{Base}/{{itemId:guid}}/media/{{mediaId:guid}}/primary";
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
            public const string SetExternalTracking = $"{Base}/inbound-shipments/{{shipmentId}}/tracking";
            public const string UpdateExternalStatus   = $"{Base}/inbound-shipments/{{shipmentId}}/status";
            public const string QrCode                 = $"{Base}/inbound-shipments/{{shipmentId:guid}}/qr";
            public const string InspectMultipart = $"{Base}/inbound-shipments/{{shipmentId}}/inspect/multipart";
            public const string SelfShipOutbound = $"{Base}/outbound-shipments/self-ship";
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