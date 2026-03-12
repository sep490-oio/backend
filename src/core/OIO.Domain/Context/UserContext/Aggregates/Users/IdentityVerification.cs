using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class IdentityVerification : AggregateRoot<IdentityVerificationId>, IAuditableEntity
{
    private readonly List<VerificationDocument> _documents = [];
    private readonly List<VerificationHistory> _history = [];

    public UserId UserId { get; private set; }
    public VerificationType VerificationType { get; private set; }
    public string? FullName { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }
    public string? Nationality { get; private set; }
    public IdentityDocument? Document { get; private set; }
    public PermanentAddress? PermanentAddress { get; private set; }
    public IdentityVerificationStatus Status { get; private set; }
    public DateTime? VerifiedAt { get; private set; }
    public UserId? VerifiedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? RejectionCode { get; private set; }
    public bool AutoVerified { get; private set; }
    public decimal? AutoVerifyScore { get; private set; }
    public string? AutoVerifyProvider { get; private set; }
    public string? AutoVerifyResponse { get; private set; } // jsonb
    public DateTime? SubmittedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<VerificationDocument> Documents => _documents.AsReadOnly();
    public IReadOnlyCollection<VerificationHistory> History => _history.AsReadOnly();

    private IdentityVerification() { }

    public static IdentityVerification Create(
        UserId userId,
        VerificationType verificationType,
        DateTime nowUtc)
    {
        var verification = new IdentityVerification
        {
            Id = IdentityVerificationId.From(Guid.CreateVersion7()),
            UserId = userId,
            VerificationType = verificationType,
            Status = IdentityVerificationStatus.Pending,
            AttemptCount = 0,
            CreatedAt = nowUtc
        };

        verification.AddHistory(
            VerificationHistoryAction.Created,
            null,
            IdentityVerificationStatus.Pending.Id,
            userId,
            PerformerType.Buyer,
            nowUtc);

        return verification;
    }

    public void PopulateFromOcr(
        string? fullName,
        DateOnly? dateOfBirth,
        Gender? gender,
        string? nationality,
        IdentityDocument? document,
        PermanentAddress? permanentAddress,
        DateTime nowUtc)
    {
        FullName = fullName?.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Nationality = nationality?.Trim();
        Document = document;
        PermanentAddress = permanentAddress;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.InfoUpdated,
            null,
            Status.Id,
            UserId,
            PerformerType.System,
            nowUtc,
            "Populated from eKYC OCR");
    }

    public UnitResult<Error> Update(
        string fullName,
        DateOnly dateOfBirth,
        Gender gender,
        IdentityDocument document,
        PermanentAddress permanentAddress,
        DateTime nowUtc,
        UserId performedBy,
        PerformerType performerType,
        string? nationality = null)
    {
        if (Status != IdentityVerificationStatus.Pending
            && Status != IdentityVerificationStatus.Rejected
            && Status != IdentityVerificationStatus.UnderReview)
            return UserErrors.Verification.CannotUpdateInCurrentStatus(Status);

        if (Status == IdentityVerificationStatus.Rejected)
        {
            Status = IdentityVerificationStatus.Pending;
            RejectionReason = null;
            RejectionCode = null;
        }

        FullName = fullName.Trim();
        DateOfBirth = dateOfBirth;
        Gender = gender;
        Nationality = nationality?.Trim();
        Document = document;
        PermanentAddress = permanentAddress;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.InfoUpdated,
            null,
            Status.Id,
            performedBy,
            performerType,
            nowUtc);

        return UnitResult.Success<Error>();
    }

    public Result<VerificationDocument, Error> AddDocument(
        VerificationDocumentType documentType,
        MediaUpload upload,
        int maxForType,
        DateTime nowUtc)
    {
        if (Status != IdentityVerificationStatus.Pending && Status != IdentityVerificationStatus.Rejected)
            return UserErrors.Verification.CannotUploadDocInCurrentStatus(Status);

        if (!upload.IsConfirmed)
            return MediaErrors.NotConfirm;

        if (string.IsNullOrWhiteSpace(upload.Info.SecureUrl))
            return MediaErrors.NotContainUrl;

        var currentCount = _documents.Count;
        if (currentCount >= maxForType)
            return UserErrors.Verification.MaxDocumentsReached(maxForType);

        var doc = VerificationDocument.Create(
            Id, documentType, upload.ResourceType, upload.StorageRef, upload.Info, nowUtc);

        _documents.Add(doc);
        ModifiedAt = nowUtc;

        var result = upload.LinkToEntity(Id.Value, nowUtc);

        if (result.IsFailure)
            return result.Error;

        AddHistory(
            VerificationHistoryAction.DocumentUploaded,
            null,
            Status.Id,
            UserId,
            PerformerType.Buyer,
            nowUtc);

        return doc;
    }

    public UnitResult<Error> RemoveDocument(
        VerificationDocumentId documentId,
        DateTime nowUtc)
    {
        if (Status != IdentityVerificationStatus.Pending && Status != IdentityVerificationStatus.Rejected)
            return UserErrors.Verification.CannotDeleteDocInCurrentStatus(Status);

        var doc = _documents.FirstOrDefault(d => d.Id == documentId);
        if (doc is null)
            return UserErrors.Verification.DocumentNotFound(documentId);

        _documents.Remove(doc);
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.DocumentDeleted,
            null,
            Status.Id,
            UserId,
            PerformerType.Buyer,
            nowUtc);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Submit(DateTime nowUtc)
    {
        if (Status != IdentityVerificationStatus.Pending)
            return UserErrors.Verification.CannotSubmitInCurrentStatus(Status);

        if (_documents.Count == 0)
            return UserErrors.Verification.NoDocumentsToSubmit;

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.Submitted;
        SubmittedAt = nowUtc;
        AttemptCount++;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.Submitted,
            oldStatus,
            Status.Id,
            UserId,
            PerformerType.Buyer,
            nowUtc);

        RaiseDomainEvent(new VerificationSubmittedEvent(
            VerificationId: Id.Value,
            UserId: UserId.Value,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Approve(
        UserId adminId,
        DateTime nowUtc,
        DateTime? expiresAt = null)
    {
        if (Status != IdentityVerificationStatus.Submitted && Status != IdentityVerificationStatus.UnderReview)
            return UserErrors.Verification.CannotApproveInCurrentStatus(Status);

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.Approved;
        VerifiedAt = nowUtc;
        VerifiedBy = adminId;
        ExpiresAt = expiresAt;
        RejectionReason = null;
        RejectionCode = null;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.Approved,
            oldStatus,
            Status.Id,
            adminId,
            PerformerType.Admin,
            nowUtc);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(
        UserId adminId,
        string reason,
        DateTime nowUtc,
        string? rejectionCode = null)
    {
        if (Status != IdentityVerificationStatus.Submitted && Status != IdentityVerificationStatus.UnderReview)
            return UserErrors.Verification.CannotRejectInCurrentStatus(Status);

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.Rejected;
        RejectionReason = reason;
        RejectionCode = rejectionCode;
        VerifiedBy = adminId;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.Rejected,
            oldStatus,
            Status.Id,
            adminId,
            PerformerType.Admin,
            nowUtc,
            reason);

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> AutoApprove(
        decimal score,
        string provider,
        string rawResponse,
        DateTime nowUtc,
        DateTime? expiresAt = null)
    {
        if (Status != IdentityVerificationStatus.Submitted)
            return UserErrors.Verification.CannotApproveInCurrentStatus(Status);

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.Approved;
        AutoVerified = true;
        AutoVerifyScore = score;
        AutoVerifyProvider = provider;
        AutoVerifyResponse = rawResponse;
        VerifiedAt = nowUtc;
        ExpiresAt = expiresAt;
        RejectionReason = null;
        RejectionCode = null;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.Approved,
            oldStatus,
            Status.Id,
            UserId,
            PerformerType.System,
            nowUtc,
            $"Auto-approved by {provider} (score: {score:F2})");

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> AutoReject(
        string reason,
        decimal score,
        string provider,
        string rawResponse,
        DateTime nowUtc,
        string? rejectionCode = null)
    {
        if (Status != IdentityVerificationStatus.Submitted)
            return UserErrors.Verification.CannotRejectInCurrentStatus(Status);

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.Rejected;
        AutoVerified = true;
        AutoVerifyScore = score;
        AutoVerifyProvider = provider;
        AutoVerifyResponse = rawResponse;
        RejectionReason = reason;
        RejectionCode = rejectionCode;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.Rejected,
            oldStatus,
            Status.Id,
            UserId,
            PerformerType.System,
            nowUtc,
            $"Auto-rejected by {provider} (score: {score:F2}): {reason}");

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MoveToReview(
        decimal score,
        string provider,
        string rawResponse,
        DateTime nowUtc)
    {
        if (Status != IdentityVerificationStatus.Submitted)
            return UserErrors.Verification.CannotApproveInCurrentStatus(Status);

        var oldStatus = Status.Id;
        Status = IdentityVerificationStatus.UnderReview;
        AutoVerified = false;
        AutoVerifyScore = score;
        AutoVerifyProvider = provider;
        AutoVerifyResponse = rawResponse;
        ModifiedAt = nowUtc;

        AddHistory(
            VerificationHistoryAction.MovedToReview,
            oldStatus,
            Status.Id,
            UserId,
            PerformerType.System,
            nowUtc,
            $"Moved to manual review by {provider} (score: {score:F2})");

        return UnitResult.Success<Error>();
    }

    private void AddHistory(
        VerificationHistoryAction action,
        string? oldStatus,
        string? newStatus,
        UserId performedBy,
        PerformerType performerType,
        DateTime nowUtc,
        string? notes = null)
    {
        _history.Add(VerificationHistory.Create(
            Id, action, oldStatus, newStatus, performedBy, performerType, nowUtc, notes));
    }
}
