using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.UserContext.Services;

public interface ITokenProvider
{
    string GenerateJwt(
        UserId userId,
        UserEmail email,
        UserName userName,
        IReadOnlyCollection<string> roles,
        DateTime now);
    
    string Generate();
}