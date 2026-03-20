using System.Globalization;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Ekyc;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.NotificationContext.Enums;
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
    private readonly VerificationDuplicateIdentityService _duplicateIdentityService;
    private readonly ISender _sender;
    private readonly ILogger<VerificationSubmittedEventHandler> _logger;

    public VerificationSubmittedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IEkycProvider ekycProvider,
        IClock clock,
        VerificationDuplicateIdentityService duplicateIdentityService,
        ISender sender,
        ILogger<VerificationSubmittedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _ekycProvider = ekycProvider;
        _clock = clock;
        _duplicateIdentityService = duplicateIdentityService;
        _sender = sender;
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

            var transitioned = await MoveToManualReviewAsync(
                verification,
                notification.VerificationId,
                JsonSerializer.Serialize(new { error = "Missing required documents" }),
                cancellationToken);

            if (transitioned)
            {
                await SendOutcomeNotificationAsync(
                    verification,
                    "verification_manual_review_required",
                    "KYC can duoc kiem tra thu cong",
                    "Xac minh danh tinh cua ban can duoc kiem tra thu cong.",
                    cancellationToken);
            }
            return;
        }

        var stateTransitionApplied = false;

        try
        {
            var ekycRequest = new EkycVerificationRequest(
                IdFrontImage: new EkycImageRef(idFront.Info.SecureUrl, idFront.StorageRef.PublicId),
                IdBackImage: idBack?.Info.SecureUrl is null
                    ? null
                    : new EkycImageRef(idBack.Info.SecureUrl, idBack.StorageRef.PublicId),
                SelfieImage: new EkycImageRef(selfie.Info.SecureUrl, selfie.StorageRef.PublicId));

            var ekycResult = await _ekycProvider.VerifyIdentityAsync(ekycRequest, cancellationToken);
            var nowUtc = _clock.UtcNow;

            if (ekycResult.IsFailure)
            {
                _logger.LogError(
                    "eKYC provider failed for verification {VerificationId}: {Error}",
                    notification.VerificationId,
                    ekycResult.Error.Message);

                stateTransitionApplied = true;
                var transitioned = await MoveToManualReviewAsync(
                    verification,
                    notification.VerificationId,
                    JsonSerializer.Serialize(new
                    {
                        error = ekycResult.Error.Message,
                        code = ekycResult.Error.Code,
                        provider = _ekycProvider.ProviderName
                    }),
                    cancellationToken);

                if (transitioned)
                {
                    await SendOutcomeNotificationAsync(
                        verification,
                        "verification_manual_review_required",
                        "KYC can duoc kiem tra thu cong",
                        "Xac minh danh tinh cua ban can duoc kiem tra thu cong.",
                        cancellationToken);
                }
                return;
            }

            var result = ekycResult.Value;

            if (result.OcrData is not null)
            {
                PopulateOcrData(verification, result.OcrData, nowUtc);
            }

            var duplicate = await _duplicateIdentityService.FindDuplicateAsync(verification, cancellationToken);
            if (duplicate is not null)
            {
                _logger.LogWarning(
                    "Duplicate identity detected for verification {VerificationId}. Duplicate verification {DuplicateVerificationId} from user {DuplicateUserId}.",
                    notification.VerificationId,
                    duplicate.VerificationId,
                    duplicate.UserId);

                EnsureSuccess(verification.AutoReject(
                    reason: "This identity document is already associated with another account.",
                    score: result.OverallScore,
                    provider: result.ProviderName,
                    rawResponse: result.RawResponse,
                    nowUtc: nowUtc,
                    rejectionCode: "DUPLICATE_IDENTITY"));

                stateTransitionApplied = true;
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await SendOutcomeNotificationAsync(
                    verification,
                    "verification_auto_rejected",
                    "KYC khong duoc chap nhan",
                    "Xac minh danh tinh cua ban khong dat. Vui long xem chi tiet va gui lai neu can.",
                    cancellationToken);
                return;
            }

            _logger.LogInformation(
                "eKYC result for verification {VerificationId}: Decision={Decision}, Score={Score}, Warnings={Warnings}",
                notification.VerificationId, result.Decision, result.OverallScore,
                string.Join(", ", result.Warnings));

            switch (result.Decision)
            {
                case EkycDecision.Approved:
                    EnsureSuccess(verification.AutoApprove(
                        score: result.OverallScore,
                        provider: result.ProviderName,
                        rawResponse: result.RawResponse,
                        nowUtc: nowUtc));
                    _logger.LogInformation("Verification {VerificationId} auto-approved (score: {Score})",
                        notification.VerificationId, result.OverallScore);
                    break;

                case EkycDecision.Rejected:
                    EnsureSuccess(verification.AutoReject(
                        reason: result.RejectionReason ?? "eKYC verification failed",
                        score: result.OverallScore,
                        provider: result.ProviderName,
                        rawResponse: result.RawResponse,
                        nowUtc: nowUtc,
                        rejectionCode: "EKYC_FAILED"));
                    _logger.LogInformation("Verification {VerificationId} auto-rejected: {Reason}",
                        notification.VerificationId, result.RejectionReason);
                    break;

                case EkycDecision.NeedsReview:
                    EnsureSuccess(verification.MoveToReview(
                        score: result.OverallScore,
                        provider: result.ProviderName,
                        rawResponse: result.RawResponse,
                        nowUtc: nowUtc));
                    _logger.LogInformation("Verification {VerificationId} moved to manual review (score: {Score})",
                        notification.VerificationId, result.OverallScore);
                    break;
            }

            stateTransitionApplied = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var eventType = result.Decision switch
            {
                EkycDecision.Approved => "verification_auto_approved",
                EkycDecision.Rejected => "verification_auto_rejected",
                _ => "verification_manual_review_required"
            };

            var title = result.Decision switch
            {
                EkycDecision.Approved => "KYC da duoc xac minh thanh cong",
                EkycDecision.Rejected => "KYC khong duoc chap nhan",
                _ => "KYC can duoc kiem tra thu cong"
            };

            var message = result.Decision switch
            {
                EkycDecision.Approved => "Xac minh danh tinh cua ban da duoc phe duyet tu dong.",
                EkycDecision.Rejected => "Xac minh danh tinh cua ban khong dat. Vui long xem chi tiet va gui lai neu can.",
                _ => "Xac minh danh tinh cua ban can duoc kiem tra thu cong."
            };

            await SendOutcomeNotificationAsync(
                verification,
                eventType,
                title,
                message,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled eKYC exception for verification {VerificationId}.",
                notification.VerificationId);

            if (stateTransitionApplied || verification.Status != IdentityVerificationStatus.Submitted)
                throw;

            var transitioned = await MoveToManualReviewAsync(
                verification,
                notification.VerificationId,
                JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    provider = _ekycProvider.ProviderName,
                    type = ex.GetType().Name
                }),
                cancellationToken);

            if (transitioned)
            {
                await SendOutcomeNotificationAsync(
                    verification,
                    "verification_manual_review_required",
                    "KYC can duoc kiem tra thu cong",
                    "Xac minh danh tinh cua ban can duoc kiem tra thu cong.",
                    cancellationToken);
            }
        }
    }

    private async Task<bool> MoveToManualReviewAsync(
        IdentityVerification verification,
        Guid verificationId,
        string rawResponse,
        CancellationToken cancellationToken)
    {
        if (verification.Status != IdentityVerificationStatus.Submitted)
        {
            _logger.LogWarning(
                "Verification {VerificationId} already transitioned to {Status} while applying manual review fallback. Skipping fallback.",
                verificationId,
                verification.Status.Id);
            return false;
        }

        EnsureSuccess(verification.MoveToReview(
            score: 0,
            provider: _ekycProvider.ProviderName,
            rawResponse: rawResponse,
            nowUtc: _clock.UtcNow));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void EnsureSuccess(CSharpFunctionalExtensions.UnitResult<OIO.Domain.SeedWork.Errors.Error> result)
    {
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error.Message);
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

    private async Task SendOutcomeNotificationAsync(
        IdentityVerification verification,
        string eventType,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: verification.UserId.Value,
                NotificationType: "verification",
                EventType: eventType,
                Title: title,
                Message: message,
                Priority: NotificationPriority.Normal,
                EntityType: "verification",
                EntityId: verification.Id.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    verificationId = verification.Id.Value,
                    userId = verification.UserId.Value,
                    provider = verification.AutoVerifyProvider ?? _ekycProvider.ProviderName,
                    status = verification.Status.Id,
                    rejectionCode = verification.RejectionCode
                })),
            cancellationToken);
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
            "ná»¯" or "nu" or "female" => Gender.Female,
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
        if (normalized.Contains("cÄƒn cÆ°á»›c") || normalized.Contains("cccd"))
            return IdType.Cccd;
        if (normalized.Contains("chá»©ng minh") || normalized.Contains("cmnd"))
            return IdType.Cmnd;
        if (normalized.Contains("passport") || normalized.Contains("há»™ chiáº¿u"))
            return IdType.Passport;

        return IdType.Cccd;
    }

    private static PermanentAddress? BuildAddress(EkycOcrData ocr)
    {
        if (string.IsNullOrWhiteSpace(ocr.Address))
            return null;

        return PermanentAddress.Create(
            fullAddress: ocr.Address,
            province: string.Join(", ", ocr.PostCode?.City ?? []),
            district: string.Join(", ", ocr.PostCode?.District ?? []),
            ward: string.Join(", ", ocr.PostCode?.Ward ?? []));
    }
}
