namespace OIO.Application.Abstractions.Auth;

public interface ITotpService
{
    string GenerateSecret();

    byte[] GenerateQrCodePng(string secret, string userEmail, string issuer);

    bool VerifyCode(string secret, string code, out long timeStepMatched);
}
