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
            PublishedAt: document.PublishedAt,
            CreatedAt: document.CreatedAt,
            ContentUrl: document.Info?.SecureUrl ?? string.Empty,
            FileName: document.Info?.FileName,
            FileSize: document.Info?.Bytes ?? 0,
            Format: document.Info?.Format,
            Width: document.Info?.Width,
            Height: document.Info?.Height,
            DurationSeconds: document.Info?.DurationSeconds,
            StoragePublicId: document.StorageRef?.PublicId,
            StorageFolder: document.StorageRef?.Folder);
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
