namespace OIO.Api.Common;



public static class ApiEndpoint
{
    public static class Tags
    {
        public const string Auth = nameof(Auth);
        public const string Users =  nameof(Users);
        public const string Admins =  nameof(Admins);
    }

    public static class Names
    {
        public static class Admins
        {
            public const string GetUser = nameof(GetUser);
            public const string ChangeUserStatus = nameof(ChangeUserStatus);
            public const string AssignRole = nameof(AssignRole);
            public const string RevokeRole = nameof(RevokeRole);
            public const string RemoveUser = nameof(RemoveUser);
            public const string GrantPermission = nameof(GrantPermission);
            public const string DenyPermission = nameof(DenyPermission);
            public const string RevokePermission = nameof(RevokePermission);
            public const string TogglePermission = nameof(TogglePermission);
            public const string UnlockUser = nameof(UnlockUser);
        }

        public static class Auth
        {
            public const string ConfirmEmail = nameof(ConfirmEmail);
            public const string Login = nameof(Login);
            public const string Logout =  nameof(Logout);
            public const string RefreshToken = nameof(RefreshToken);
            public const string Register = nameof(Register);

        }

        public static class Users
        {
            public const string AddAddress = nameof(AddAddress);
            public const string ChangePassword = nameof(ChangePassword);
            public const string ConfirmPhoneNumber = nameof(ConfirmPhoneNumber);
            public const string DisableTwoFactor = nameof(DisableTwoFactor);
            public const string EnableTwoFactor = nameof(EnableTwoFactor);
            public const string GetActiveSessions = nameof(GetActiveSessions);
            public const string GetAddresses = nameof(GetAddresses);
            public const string GetCurrentUser = nameof(GetCurrentUser);
            public const string GetCurrentUserProfile = nameof(GetCurrentUserProfile);
            public const string GetLoginHistory = nameof(GetLoginHistory);
            public const string RemoveAddress = nameof(RemoveAddress);
            public const string SetDefaultAddress = nameof(SetDefaultAddress);
            public const string SetPhoneNumber = nameof(SetPhoneNumber);
            public const string UpdateAddress = nameof(UpdateAddress);
            public const string UpdateCurrentUserProfile = nameof(UpdateCurrentUserProfile);
        }
    }

    public static class Url
    {
        public static class Admins
        {
            public const string GetUser = "api/admin/users/{userId:guid}";
            public const string AssignRole = "api/admin/users/{userId:guid}/roles/{roleId:int}";
            public const string RevokeRole = "api/admin/users/{userId:guid}/roles/{roleId:int}";
            public const string ChangeUserStatus = "api/admin/users/{userId:guid}/status";
            public const string RemoveUser = "api/admin/users/{userId:guid}";
            public const string GrantPermission = "api/admin/users/{userId:guid}/permissions/{permissionId:int}";
            public const string DenyPermission = "api/admin/users/{userId:guid}/permissions/{permissionId:int}";
            public const string RevokePermission = "api/admin/users/{userId:guid}/permissions/{permissionId:int}";
            public const string UnlockUser = "api/admin/users/{userId:guid}/unlock";
            public const string TogglePermission = "api/admin/roles/{roleId:int}/permissions/{permissionId:int}";
        }

        public static class Auth
        {
            public const string ConfirmEmail = "api/auth/confirm-email";
            public const string Login = "api/auth/login";
            public const string Logout =  "api/auth/logout";
            public const string RefreshToken = "api/auth/refresh";
            public const string Register = "api/auth/register";

        }

        public static class Users
        {
            public const string AddAddress = "api/users/me/addresses";
            public const string ChangePassword = "api/users/me/password";
            public const string ConfirmPhoneNumber = "api/users/me/phone/confirm";
            public const string DisableTwoFactor = "api/users/me/two-factor/disable";
            public const string EnableTwoFactor = "api/users/me/two-factor/enable";
            public const string GetActiveSessions = "api/users/me/sessions";
            public const string GetAddresses = "api/users/me/addresses";
            public const string GetCurrentUser = "api/users/me";
            public const string GetCurrentUserProfile = "api/users/me/profile";
            public const string GetLoginHistory = "api/users/me/login-history";
            public const string RemoveAddress = "api/users/me/addresses/{addressId:guid}";
            public const string SetDefaultAddress = "api/users/me/addresses/{addressId:guid}/default";
            public const string SetPhoneNumber = "api/users/me/phone";
            public const string UpdateAddress = "api/users/me/addresses/{addressId:guid}";
            public const string UpdateCurrentUserProfile = "api/users/me/profile";
        }
    }
    


}