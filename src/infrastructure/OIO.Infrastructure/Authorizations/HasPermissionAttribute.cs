using Microsoft.AspNetCore.Authorization;

namespace OIO.Infrastructure.Authorizations;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(permission)
    {
        Policy = permission; 
    }
}