using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

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
    
    private TermsAcceptance(
        TermsAcceptanceId id,
        UserId userId,
        TermsDocumentId termDocumentId,
        DateTime acceptedAt,
        IPAddress? ipAddress,
        string? userAgent)
    {
        Id = id;
        UserId = userId;
        TermDocumentId = termDocumentId;
        AcceptedAt = acceptedAt;
        IpAddress = ipAddress;
        UserAgent = userAgent?.Trim();
    }

    public static Result<TermsAcceptance, Error> Create(
        UserId userId,
        TermsDocumentId termDocumentId,
        DateTime acceptedAt,
        IPAddress? ipAddress,
        string? userAgent)
    {
        return new TermsAcceptance(
            TermsAcceptanceId.From(Guid.CreateVersion7()),
            userId,
            termDocumentId,
            acceptedAt,
            ipAddress,
            userAgent);
    }
}