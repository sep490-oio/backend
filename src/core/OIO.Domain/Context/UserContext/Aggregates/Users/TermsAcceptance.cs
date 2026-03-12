using System.Net;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.UserContext.Aggregates.Users;

/// <summary>
/// Maps to: user_terms_acceptances
/// UNIQUE(user_id, term_document_id)
/// </summary>
public sealed class TermsAcceptance : BaseEntity<TermsAcceptanceId>
{
    public UserId UserId { get; private set; }
    public TermsDocumentId TermDocumentId { get; private set; }
    public DateTime AcceptedAt { get; private set; }
    public IPAddress? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    // Navigation
    public TermsDocument TermDocument { get; private set; } = null!;

    private TermsAcceptance() { }
}