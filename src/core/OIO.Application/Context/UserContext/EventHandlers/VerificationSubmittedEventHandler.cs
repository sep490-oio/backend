using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Ekyc;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.UserContext.EventHandlers;

internal sealed class VerificationSubmittedEventHandler
    : INotificationHandler<VerificationSubmittedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEkycProvider _ekycProvider;
    private readonly IClock _clock;
    private readonly ILogger<VerificationSubmittedEventHandler> _logger;

    public VerificationSubmittedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IEkycProvider ekycProvider,
        IClock clock,
        ILogger<VerificationSubmittedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _ekycProvider = ekycProvider;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(
        VerificationSubmittedEvent notification,
        CancellationToken cancellationToken)
    {
        var verificationId = IdentityVerificationId.From(notification.VerificationId);

        _logger.LogInformation(
            "Processing eKYC for verification {VerificationId}, user {UserId}",
            notification.VerificationId, notification.UserId);

        var verification = await _dbContext.GetByIdAsync<IdentityVerification, IdentityVerificationId>(
            verificationId,
            q => q.Include(v => v.Documents),
            cancellationToken);

        if (verification is null)
        {
            _logger.LogWarning("Verification {VerificationId} not found for eKYC processing", notification.VerificationId);
            return;
        }

        if (verification.Status != IdentityVerificationStatus.Submitted)
        {
            _logger.LogWarning(
                "Verification {VerificationId} is in status {Status}, expected Submitted. Skipping eKYC.",
                notification.VerificationId, verification.Status.Id);
            return;
        }

        var idFront = verification.Documents
            .FirstOrDefault(d => d.DocumentType == VerificationDocumentType.IdFront);
        var idBack = verification.Documents
            .FirstOrDefault(d => d.DocumentType == VerificationDocumentType.IdBack);
        var selfie = verification.Documents
            .FirstOrDefault(d => d.DocumentType == VerificationDocumentType.Selfie);

        if (idFront?.Info.SecureUrl is null || selfie?.Info.SecureUrl is null)
        {
            _logger.LogWarning(
                "Verification {VerificationId} missing required documents (id_front or selfie). Moving to manual review.",
                notification.VerificationId);

            verification.MoveToReview(
                score: 0,
                provider: _ekycProvider.ProviderName,
                rawResponse: "{\"error\":\"Missing required documents\"}",
                nowUtc: _clock.UtcNow);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var ekycRequest = new EkycVerificationRequest(
            IdFrontImageUrl: idFront.Info.SecureUrl,
            IdBackImageUrl: idBack?.Info.SecureUrl,
            SelfieImageUrl: selfie.Info.SecureUrl);

        var ekycResult = await _ekycProvider.VerifyIdentityAsync(ekycRequest, cancellationToken);

        var nowUtc = _clock.UtcNow;

        if (ekycResult.IsFailure)
        {
            _logger.LogError(
                "eKYC provider failed for verification {VerificationId}: {Error}",
                notification.VerificationId, ekycResult.Error.Message);

            verification.MoveToReview(
                score: 0,
                provider: _ekycProvider.ProviderName,
                rawResponse: $"{{\"error\":\"{ekycResult.Error.Message}\"}}",
                nowUtc: nowUtc);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var result = ekycResult.Value;

        if (result.OcrData is not null)
        {
            PopulateOcrData(verification, result.OcrData, nowUtc);
        }

        _logger.LogInformation(
            "eKYC result for verification {VerificationId}: Decision={Decision}, Score={Score}, Warnings={Warnings}",
            notification.VerificationId, result.Decision, result.OverallScore,
            string.Join(", ", result.Warnings));

        switch (result.Decision)
        {
            case EkycDecision.Approved:
                verification.AutoApprove(
                    score: result.OverallScore,
                    provider: result.ProviderName,
                    rawResponse: result.RawResponse,
                    nowUtc: nowUtc);
                _logger.LogInformation("Verification {VerificationId} auto-approved (score: {Score})",
                    notification.VerificationId, result.OverallScore);
                break;

            case EkycDecision.Rejected:
                verification.AutoReject(
                    reason: result.RejectionReason ?? "eKYC verification failed",
                    score: result.OverallScore,
                    provider: result.ProviderName,
                    rawResponse: result.RawResponse,
                    nowUtc: nowUtc,
                    rejectionCode: "EKYC_FAILED");
                _logger.LogInformation("Verification {VerificationId} auto-rejected: {Reason}",
                    notification.VerificationId, result.RejectionReason);
                break;

            case EkycDecision.NeedsReview:
                verification.MoveToReview(
                    score: result.OverallScore,
                    provider: result.ProviderName,
                    rawResponse: result.RawResponse,
                    nowUtc: nowUtc);
                _logger.LogInformation("Verification {VerificationId} moved to manual review (score: {Score})",
                    notification.VerificationId, result.OverallScore);
                break;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void PopulateOcrData(
        IdentityVerification verification, EkycOcrData ocr, DateTime nowUtc)
    {
        var dateOfBirth = TryParseDate(ocr.DateOfBirth);
        var gender = MapGender(ocr.Gender);
        var document = BuildIdentityDocument(ocr);
        var address = BuildAddress(ocr);

        verification.PopulateFromOcr(
            fullName: ocr.FullName,
            dateOfBirth: dateOfBirth,
            gender: gender,
            nationality: ocr.Nationality,
            document: document,
            permanentAddress: address,
            nowUtc: nowUtc);
    }

    private static DateOnly? TryParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        string[] formats = ["dd/MM/yyyy", "yyyy-MM-dd", "dd-MM-yyyy"];
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : null;
    }

    private static Gender? MapGender(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "nam" or "male" => Gender.Male,
            "nữ" or "nu" or "female" => Gender.Female,
            _ => Gender.FromId(normalized).GetValueOrDefault()
        };
    }

    private static IdentityDocument? BuildIdentityDocument(EkycOcrData ocr)
    {
        if (string.IsNullOrWhiteSpace(ocr.IdNumber)) return null;

        var idType = MapIdType(ocr.CardType);
        var issueDate = TryParseDate(ocr.IssueDate);
        var expiryDate = TryParseDate(ocr.ExpiryDate);

        var result = IdentityDocument.Create(
            idType, ocr.IdNumber, issueDate, expiryDate, ocr.IssuePlace);

        return result.IsSuccess ? result.Value : null;
    }

    private static IdType MapIdType(string? cardType)
    {
        if (string.IsNullOrWhiteSpace(cardType)) return IdType.Cccd;

        var normalized = cardType.Trim().ToLowerInvariant();
        if (normalized.Contains("căn cước") || normalized.Contains("cccd"))
            return IdType.Cccd;
        if (normalized.Contains("chứng minh") || normalized.Contains("cmnd"))
            return IdType.Cmnd;
        if (normalized.Contains("passport") || normalized.Contains("hộ chiếu"))
            return IdType.Passport;

        return IdType.Cccd;
    }

    private static PermanentAddress? BuildAddress(EkycOcrData ocr)
    {
        if (string.IsNullOrWhiteSpace(ocr.Address)) return null;

        return PermanentAddress.Create(
            fullAddress: ocr.Address,
            province: "",
            district: "",
            ward: "");
    }
}
