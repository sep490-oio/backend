using OIO.Application.Abstractions.Auth;
using OtpNet;
using QRCoder;

namespace OIO.Infrastructure.Auth;

public sealed class TotpService : ITotpService
{
    public string GenerateSecret()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public byte[] GenerateQrCodePng(string secret, string userEmail, string issuer)
    {
        var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(userEmail)}" +
                  $"?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";

        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(uri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(5);
    }

    public bool VerifyCode(string secret, string code, out long timeStepMatched)
    {
        var key = Base32Encoding.ToBytes(secret);
        var totp = new Totp(key);

        return totp.VerifyTotp(code, out timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);
    }
}
