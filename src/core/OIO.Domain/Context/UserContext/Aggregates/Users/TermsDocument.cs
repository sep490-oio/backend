using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

public sealed class TermsDocument : BaseEntity<TermsDocumentId>, ICreatedAtEntity
{
    public string TermType { get; private set; }
    public int Version { get; private set; }
    public StorageRef StorageRef { get; private set; }
    public MediaInfo Info { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private TermsDocument() { }
}