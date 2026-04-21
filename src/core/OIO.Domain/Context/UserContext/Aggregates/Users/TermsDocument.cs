using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users.Events;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

// NOTE: does NOT implement IVersionEntity. The `Version` property below is the
// document CONTENT version (v1, v2, v3) — not an EF concurrency token. The
// AuditableEntityInterceptor auto-increments Version for IVersionEntity on every
// Modified save; that would collide with the unique (term_type, version) index
// whenever a non-version field (status, archived_at, ...) is updated.
public sealed class TermsDocument : AggregateRoot<TermsDocumentId>, ICreatedAtEntity
{
    public string TermType { get; private set; }
    public int Version { get; private set; }
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public TermsDocumentStatus Status { get; private set; } = TermsDocumentStatus.Draft;
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Audit fields — populated by Create / Activate / Archive.
    public UserId? CreatedBy { get; private set; }
    public UserId? ActivatedBy { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public UserId? ArchivedBy { get; private set; }
    public string? ArchivedReason { get; private set; }

    /// <summary>
    /// Legacy compat surface — one-release bridge while callers migrate to <see cref="Status"/>.
    /// Computed from <see cref="Status"/>; no backing setter. The physical <c>is_active</c> column
    /// remains on the table through Phase G and is dropped in Phase H (see ralplan §3.7, H1/H2).
    /// </summary>
    public bool IsActive => Status == TermsDocumentStatus.Active;

    private TermsDocument() { }

    private TermsDocument(
        TermsDocumentId id,
        string termType,
        int version,
        StorageRef storageRef,
        MediaInfo info,
        UserId createdBy,
        DateTime createdAt)
    {
        Id = id;
        TermType = termType;
        Version = version;
        StorageRef = storageRef;
        Info = info;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
        Status = TermsDocumentStatus.Draft;
    }

    public static Result<TermsDocument, Error> Create(
        string termType,
        int version,
        MediaUpload upload,
        UserId createdBy,
        DateTime nowUtc)
    {
        var check = TermsDocument.Check(isInvariant: true)
            .Field(termType)
            .NotWhiteSpace()
            .Field(upload.Info.SecureUrl)
            .NotNullOrWhiteSpace()
            .Field(upload.StorageRef.PublicId)
            .NotWhiteSpace()
            .Field(upload.StorageRef.Folder)
            .NotWhiteSpace()
            .Field(version)
            .GreaterThanOrEqual(1)
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        var termsDocument = new TermsDocument(
            TermsDocumentId.From(Guid.CreateVersion7()),
            termType.Trim(),
            version,
            upload.StorageRef,
            upload.Info,
            createdBy,
            nowUtc);

        var result = upload.LinkToEntity(termsDocument.Id, nowUtc);

        if (result.IsFailure)
            return result.Error;

        return termsDocument;
    }

    /// <summary>
    /// Transitions a <c>Draft</c> document to <c>Active</c>.
    /// <para>
    /// Idempotency: a replay on a document already <c>Active</c> returns <c>Success</c> without
    /// mutating state or raising an event (per ralplan §3.1). An <c>Archived</c> document
    /// rejects with <c>TermsDocument.InvalidState</c>.
    /// </para>
    /// <para>
    /// Sibling-conflict check (an Active sibling of the same <see cref="TermType"/> already exists)
    /// lives in the command handler — see plan B1 Acceptance criterion. The domain only knows
    /// about itself.
    /// </para>
    /// </summary>
    public UnitResult<Error> Activate(DateTime nowUtc, UserId activatedBy, TermsDocumentId? supersededId = null)
    {
        if (Status == TermsDocumentStatus.Active)
        {
            // Idempotent replay — no state change, no event.
            return UnitResult.Success<Error>();
        }

        if (Status == TermsDocumentStatus.Archived)
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                "Archived documents cannot be re-activated.");
        }

        if (!Status.CanTransitionTo(TermsDocumentStatus.Active))
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                $"Cannot activate from status '{Status.Id}'.");
        }

        Status = TermsDocumentStatus.Active;
        ActivatedBy = activatedBy;
        PublishedAt ??= nowUtc;

        RaiseDomainEvent(new TermsDocumentActivatedEvent(
            TermsDocumentId: $"{Id}",
            TermType: TermType,
            NewVersion: Version,
            SupersededId: supersededId is null ? null : $"{supersededId}",
            ActivatedBy: $"{activatedBy}",
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Archives an <c>Active</c> document. Terminal state — no transitions allowed out of <c>Archived</c>.
    /// Reason is optional (Q1 decision in ralplan §6); command handler auto-generates
    /// <c>"Superseded by v{N}"</c> for sibling archival.
    /// </summary>
    public UnitResult<Error> Archive(DateTime nowUtc, UserId archivedBy, string? reason)
    {
        if (Status != TermsDocumentStatus.Active)
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                $"Only active terms documents can be archived (current status: '{Status.Id}').");
        }

        Status = TermsDocumentStatus.Archived;
        ArchivedAt = nowUtc;
        ArchivedBy = archivedBy;
        ArchivedReason = reason;

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Replaces the PDF on a <c>Draft</c> document. Version is NOT incremented (Q3 decision in
    /// ralplan §6 — only <see cref="Activate"/> commits version). Rejects on non-Draft.
    /// </summary>
    public UnitResult<Error> UpdateDraftContent(MediaUpload newUpload, DateTime nowUtc)
    {
        if (Status != TermsDocumentStatus.Draft)
        {
            return Error.Conflict(
                "TermsDocument.InvalidState",
                $"Only draft terms documents can be edited (current status: '{Status.Id}').");
        }

        StorageRef = newUpload.StorageRef;
        Info = newUpload.Info;

        var linkResult = newUpload.LinkToEntity(Id, nowUtc);
        if (linkResult.IsFailure)
            return linkResult.Error;

        return UnitResult.Success<Error>();
    }

    public void RefreshMediaSnapshot(StorageRef storageRef, MediaInfo info)
    {
        StorageRef = storageRef;
        Info = info;
    }
}
