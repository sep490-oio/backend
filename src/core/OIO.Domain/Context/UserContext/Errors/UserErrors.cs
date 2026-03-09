using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Errors;

public static class UserErrors
{
    public static class Auth
    {
        public static readonly Error AuthenticationRequired = Error.Unauthorized(
            code: "Auth.Authentication.Required",
            description: "Authentication is required to access this resource."
        );

        public static readonly Error InsufficientPermissions = Error.Forbidden(
            code: "Auth.Insufficient.Permissions",
            description: "You do not have permission to access this resource.");
        
        public static readonly Error AuthTokenMissing = Error.Unauthorized(
            code: "Auth.Token.Missing",
            description: "Access token is required."
        );

        public static readonly Error AuthTokenInvalid = Error.Unauthorized(
            code: "Auth.Token.Invalid",
            description: "Access token is invalid."
        );

        public static readonly Error AuthTokenRevoked = Error.Unauthorized(
            code: "Auth.Token.Revoked",
            description: "Access token is revoked."
        );

        // 401 - Token hết hạn
        public static readonly Error AuthTokenExpired = Error.Unauthorized(
            code: "Auth.Token.Expired",
            description: "Access token has expired."
        );

        public static readonly Error UserNotLoggedIn = Error.Unauthorized(
            code: "Auth.User.NotLoggedIn",
            description: "User is not logged in."
        );
        
        public static readonly Error SessionNotFound = Error.NotFound(
            code: "Auth.Session.NotFound",
            description: "The specified session was not found."
        );
        
          
        public static readonly Error SessionCompromised =  Error.Forbidden(
            code: "Auth.Session.Compromised",
            description: $"Token reuse detected. All sessions revoked for security."
        );
    }
    
    #region User
    public static class User
    {
        public static readonly Error UserDeleted = Error.Forbidden(
            code: "User.Deleted",
            description: "User has been deleted."
        );

        public static readonly Func<UserId, DateTime?, string?, Error> Locked = (userId, lockoutEnd, reason) => Error.Forbidden(
            code: "User.Locked",
            description: string.Join(" ", $"User '{userId}' is locked out until {lockoutEnd:O}.", reason).TrimEnd()
        );
        
        public static readonly Error PhoneNotSet = Error.Forbidden(
            code: "User.PhoneNumber.NotSet",
            description: "No phone number has been set for this user."
        );

        public static readonly Error PhoneNumberNotConfirmed = Error.Forbidden(
            code: "User.PhoneNumber.NotConfirmed",
            description: "Please confirm your phone number."
        );
        
        public static readonly Error EmailNotConfirmed = Error.Forbidden(
            code: "User.Email.NotConfirmed",
            description: "The email address has not been confirmed."
        );
        
        public static readonly Error EmailAlreadyConfirmed = Error.Forbidden(
            code: "User.Email.Confirmed",
            description: "The email address has been confirmed."
        );

        public static readonly ValidationError InvalidTwoFactorProvider = Error.Validation(
            propertyName: nameof(Aggregates.Users.User.TwoFactorProvider),
            code: "User.TwoFactorProvider.Invalid",
            description: "Two factor provider is invalid."
        );

        public static readonly Func<int, ValidationError> MaxAddressesReached = maxAddress => Error.Validation(
            propertyName: nameof(Aggregates.Users.User.Addresses),
            code: "UserMaxAddress",
            description: $"You have reach {maxAddress} that you can have for this profile."
        );
        
        public static readonly Func<UserEmail, Error> NotFoundByEmail = email => Error.NotFound(
            code: "User.NotFound",
            description: $"User with email '{email}' was not found."
        );
        
        public static readonly Func<UserId, Error> NotFound = userId => Error.NotFound(
            code: "User.NotFound",
            description: $"User with id '{userId}' was not found."
        );

        public static readonly Error EmailAlreadyExists = Error.Conflict(
            code: "User.Email.AlreadyExists",
            description: "A user with this email already exists."
        );

        public static readonly Error UserNameAlreadyExists = Error.Conflict(
            code: "User.UserName.AlreadyExists",
            description: "A user with this username already exists.");

        public static readonly Error InvalidCredentials = Error.Unauthorized(
             code: "User.Credentials.Invalid",
             description: "The provided email or password is incorrect."
        );

        public static readonly Error UserInactive = Error.Forbidden( 
            code: "User.Inactive",
            description: "The user is not active."
        );
        
        public static readonly Error UserLocked = Error.Forbidden( 
            code: "User.Locked",
            description: "The user is locked due to suspension."
        );

        public static readonly Error InvalidConfirmationToken = Error.Unauthorized(
            code: "User.ConfirmationToken.Invalid",
            description: "The confirmation token is invalid or expired."
        );
        
        public static readonly Error InvalidConfirmationCode = Error.Unauthorized(
            code: "User.ConfirmationCode.Invalid",
            description: "The confirmation code is invalid or expired."
        );
        
        public static readonly Func<UserAddressId, Error> AddressNotFound = addressId => Error.NotFound(
            code: "User.Address.NotFound",
            description: $"User address with id {addressId} not found."
        );
        
        public static readonly Error ProfileNotFound = Error.NotFound(
            code: "User.Profile.NotFound",
            description: "User profile has not been initialized."
        );
        
        public static readonly Error DeviceMismatch = Error.Unauthorized(
            code: "User.DeviceMismatch",
            description: "The request originated from a different device. Session revoked for security."
        );
        
        public static readonly Error SessionNoLongerActive = Error.Unauthorized(
            code: "User.Session.Inactive",
            description: "Session is no longer active."
        );
        
        public static readonly Error SessionAbsoluteExpired =  Error.Forbidden(
            code: "User.Session.Expired",
            description: $"Session has reached its maximum lifetime. Please login again."
        );
        
        public static readonly Error SessionSlidingExpired =  Error.Forbidden(
            code: "User.Session.Expired",
            description: $"Your session expired due to inactivity. Please login again."
        );

        public static readonly Error CannotRevokeRoleYourself = Error.Forbidden(
            code: "User.Role.Revoke.Self",
            description: "You cannot revoke your own role."
        );
        
        
        public static readonly Error CannotChangeOwnStatus = Error.Forbidden(
            code: "User.Status.Change.Self",
            description: "You cannot change your own status."
        );
        
        public static readonly Error CannotRemoveYourself = Error.Forbidden(
            code: "User.Remove.Self",
            description: "You cannot remove your own account."
        );
        
        public static readonly Error CannotUnlockYourself = Error.Forbidden(
            code: "User.Unlock.Self",
            description: "You cannot unlock your own account."
        );
        
        public static readonly Error CannotAssignRoleYourself = Error.Forbidden(
            code: "User.Role.Assign.Self",
            description: "You cannot assign a role to yourself."
        );
        
        public static readonly Error CannotManageOwnPermissions = Error.Forbidden(
            code: "User.Permission.Manage.Self",
            description: "You cannot manage permission of yourself."
        );
        
        
        public static readonly Error CannotRevokeLastAdminUser = Error.Forbidden(
            code: "User.Role.LastAdmin.Revoke",
            description: "The last admin user cannot be revoked."
        );
        
        public static readonly Error InsufficientRoleLevel = Error.Forbidden(
            code: "User.Role.Insufficient.Level",
            description: "You cannot manage this user because their role level is equal to or higher than yours.");
    }
    #endregion

    #region RefreshToken

    public static class RefreshToken
    {
        public static readonly Error Revoked = Error.Unauthorized(
            code: "User.Session.Token.Revoked",
            description: "Token has been revoked.");

        public static readonly Error Expired = Error.Unauthorized(
            code: "User.Session.Token.Expired",
            description: "Token has expired."
        );
        
        public static readonly Error Invalid = Error.Unauthorized(
            code: "User.Session.Token.Invalid",
            description: "The refresh token is invalid or expired."
        );
        
        public static readonly Error NotInSession = Error.Unauthorized(
            code: "User.Session.Token.NotIn",
            description: "Token does not belong to this session."
        );
    } 
    #endregion

    #region Permission

    public static class Permission
    {
        public static readonly Func<string, Error> NotFound = permissionId => Error.NotFound(
            code: "Permission.NotFound",
            description: $"Permission with id '{permissionId}' was not found."
        );

    }

    #endregion
}