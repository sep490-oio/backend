using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.UserContext.Services;

public interface ICurrentUser
{
    UserId UserId { get; }
    UserName? UserName { get; }
    UserEmail? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string roleName);
}