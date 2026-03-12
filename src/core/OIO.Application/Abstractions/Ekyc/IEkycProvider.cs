using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Abstractions.Ekyc;

/// <summary>
/// Vendor-agnostic eKYC provider interface.
/// Implementations: VnptEkycProvider, FptAiEkycProvider, etc.
/// </summary>
public interface IEkycProvider
{
    string ProviderName { get; }

    Task<Result<EkycVerificationResult, Error>> VerifyIdentityAsync(
        EkycVerificationRequest request,
        CancellationToken ct = default);
}

public sealed record EkycVerificationRequest(
    string IdFrontImageUrl,
    string? IdBackImageUrl,
    string SelfieImageUrl);

public sealed record EkycVerificationResult
{
    public required EkycDecision Decision { get; init; }
    public required decimal OverallScore { get; init; }
    public required string ProviderName { get; init; }
    public EkycOcrData? OcrData { get; init; }
    public decimal FaceMatchScore { get; init; }
    public bool IsCardLive { get; init; }
    public bool IsFaceLive { get; init; }
    public string? RejectionReason { get; init; }
    public List<string> Warnings { get; init; } = [];
    public string RawResponse { get; init; } = "{}";
}

public enum EkycDecision
{
    Approved,
    Rejected,
    NeedsReview
}

public sealed record EkycOcrData
{
    public string? IdNumber { get; init; }
    public string? FullName { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Gender { get; init; }
    public string? Nationality { get; init; }
    public string? Ethnicity { get; init; }
    public string? Address { get; init; }
    public string? Hometown { get; init; }
    public string? IssueDate { get; init; }
    public string? IssuePlace { get; init; }
    public string? ExpiryDate { get; init; }
    public string? CardType { get; init; }
    public bool IsIdFake { get; init; }
    public bool IsTampered { get; init; }
}
