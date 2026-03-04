using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Errors;

public static class RoleErrors
{
    public static class Role
    {
        public static readonly Error CannotInactivePermissionOfHigherOrEqualRole = Error.Forbidden(
            "Role.CannotInactivePermissionOfHigherOrEqualRole",
            "Cannot inactive permission of higher or equal role."
        );
        
        public static readonly Error CannotActivePermissionOfHigherOrEqualRole = Error.Forbidden(
            "Role.CannotActivePermissionOfHigherOrEqualRole",
            "Cannot active permission of higher or equal role."
        );
        
        public static readonly Func<RoleId, Error> NotFound = id => Error.NotFound(
            "Role.NotFound",
            $"Role with {id} not found."
        );
        
        public static readonly Error CannotAssignHigherOrEqualRole = Error.Forbidden(
            "Role.CannotAssignHigherOrEqualRole",
            "Cannot assign a role with higher or equal level than the actor's highest role."
        );
        public static readonly Error CannotRevokeHigherOrEqualRole = Error.Forbidden(
            "Role.CannotRevokeHigherOrEqualRole",
            "Cannot revoke a role with higher or equal level than the actor's highest role."
        );
    }
}