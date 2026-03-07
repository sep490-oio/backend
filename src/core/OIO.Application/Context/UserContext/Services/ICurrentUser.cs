using System.Security.Claims;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.Services;

public interface ICurrentUser
{
    UserId UserId { get; }
    UserName? UserName { get; }
    UserEmail? Email { get; }
    Guid DeviceId { get; }
    Claim[] Claims { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleName);
}