using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

internal static class TermsMappings
{
    public static TermsDocumentDto ToDto(this TermsDocument document)
    {
        return new TermsDocumentDto(
            Id: document.Id.Value,
            Type: document.TermType,
            Version: document.Version,
            IsActive: document.IsActive,
            Status: document.Status.Id,
            PublishedAt: document.PublishedAt,
            CreatedAt: document.CreatedAt,
            CreatedBy: document.CreatedBy?.Value,
            ActivatedBy: document.ActivatedBy?.Value,
            ArchivedAt: document.ArchivedAt,
            ArchivedBy: document.ArchivedBy?.Value,
            ArchivedReason: document.ArchivedReason,
            ContentUrl: NormalizePdfUrl(
                document.Info?.SecureUrl,
                document.StorageRef?.PublicId,
                document.Info?.FileName,
                document.Info?.Format),
            FileName: document.Info?.FileName,
            FileSize: document.Info?.Bytes ?? 0,
            Format: document.Info?.Format,
            Width: document.Info?.Width,
            Height: document.Info?.Height,
            DurationSeconds: document.Info?.DurationSeconds,
            StoragePublicId: document.StorageRef?.PublicId,
            StorageFolder: document.StorageRef?.Folder);
    }

    /// <summary>
    /// Defensively appends a ".pdf" extension to legacy term document URLs whose
    /// Cloudinary public_id was created before the .pdf-preserving fix. Never
    /// mutates URLs that already have a recognizable file extension.
    /// </summary>
    private static string NormalizePdfUrl(string? secureUrl, string? publicId, string? fileName, string? format)
    {
        if (string.IsNullOrWhiteSpace(secureUrl))
            return string.Empty;

        // Already ends with .pdf — trust it.
        if (secureUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return secureUrl;

        // If the URL already has some other well-known extension, don't touch it.
        var lastSegment = secureUrl.Split('?')[0].Split('/').LastOrDefault() ?? string.Empty;
        var hasExtension = lastSegment.Contains('.', StringComparison.Ordinal);
        if (hasExtension)
            return secureUrl;

        // Only append .pdf if we have positive signal that the source is a PDF.
        var looksLikePdf =
            string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(fileName) && fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(publicId) && publicId.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

        return looksLikePdf ? secureUrl + ".pdf" : secureUrl;
    }

    public static TermsAcceptanceDto ToDto(this TermsAcceptance acceptance)
    {
        return new TermsAcceptanceDto(
            Id: acceptance.Id.Value,
            AcceptedAt: acceptance.AcceptedAt,
            IpAddress: acceptance.IpAddress?.ToString(),
            UserAgent: acceptance.UserAgent,
            Document: acceptance.TermDocument?.ToDto()!);
    }
}
