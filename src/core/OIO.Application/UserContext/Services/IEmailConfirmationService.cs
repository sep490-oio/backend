using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.UserContext.Services;

public interface IEmailConfirmationService
{
    Task<string> GenerateTokenAsync(UserId userId, CancellationToken ct = default);
    Task<bool> ValidateTokenAsync(UserId userId, string token, CancellationToken ct = default);
}