using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Application.UserContext.Services;

public interface IPhoneVerificationService
{
    Task SendCodeAsync(UserId userId, PhoneNumber phoneNumber, CancellationToken ct = default);
    Task<bool> VerifyCodeAsync(UserId userId, PhoneNumber phoneNumber, string code, CancellationToken ct = default);
}