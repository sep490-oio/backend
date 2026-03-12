using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class TermsDocument : BaseEntity<TermsDocumentId>, ICreatedAtEntity, IVersionEntity
{
    public string TermType { get; private set; }
    public int Version { get; private set; }
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TermsDocument() { }
    
    private TermsDocument(
        TermsDocumentId id,
        string termType,
        int version,
        StorageRef storageRef,
        MediaInfo info,
        DateTime createdAt)
    {
        Id = id;
        TermType = termType;
        Version = version;
        StorageRef = storageRef;
        Info = info;
        CreatedAt = createdAt;
        IsActive = false;
    }
    public static Result<TermsDocument, Error> Create(
        string termType,
        int version,
        MediaUpload upload,
        DateTime nowUtc)
    {
        var check = TermsDocument.Check(isInvariant: true)
            .Field(termType)
            .NotWhiteSpace()
            .Field(upload.Info.SecureUrl)
            .NotWhiteSpace()
            .Field(upload.StorageRef.PublicId)
            .NotWhiteSpace()
            .Field(upload.StorageRef.Folder)
            .NotWhiteSpace()
            .Field(version)
            .NonNegative()
            .ToUnitResult();

        if (check.IsFailure)
        {
            return check.Error;
        }

        var termsDocument = new TermsDocument(
            TermsDocumentId.From(Guid.CreateVersion7()),
            termType.Trim(),
            0,
            upload.StorageRef,
            upload.Info,
            nowUtc);
        
        var result = upload.LinkToEntity(termsDocument.Id.Value, nowUtc);
        
        if (result.IsFailure)
            return result.Error;
        
        return termsDocument;
    }

    public UnitResult<Error> Activate(DateTime nowUtc)
    {
        IsActive = true;
        PublishedAt ??= nowUtc;
        return UnitResult.Success<Error>();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}