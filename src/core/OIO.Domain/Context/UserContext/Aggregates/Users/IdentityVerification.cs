using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class IdentityVerification : AggregateRoot<IdentityVerificationId>, IAuditableEntity
{
    private readonly List<VerificationDocument> _documents = [];
    private readonly List<VerificationHistory> _history = [];

    public UserId UserId { get; private set; }
    public VerificationType VerificationType { get; private set; }
    public string FullName { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string? Nationality { get; private set; }
    public IdentityDocument Document { get; private set; }
    public PermanentAddress PermanentAddress { get; private set; }
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
}