using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

public static class VerificationMappings
{
    public static VerificationDto ToDto(this IdentityVerification v)
    {
        return new VerificationDto(
            Id: v.Id.Value,
            UserId: v.UserId.Value,
            VerificationType: v.VerificationType.Id,
            FullName: v.FullName,
            DateOfBirth: v.DateOfBirth,
            Gender: v.Gender?.Id,
            Nationality: v.Nationality,
            Document: v.Document is not null
                ? new VerificationDocumentInfoDto(
                    IdType: v.Document.IdType.Id,
                    IdNumber: v.Document.IdNumber,
                    IssuedDate: v.Document.IssuedDate,
                    ExpiredDate: v.Document.ExpiredDate,
                    IssuedPlace: v.Document.IssuedPlace)
                : null,
            PermanentAddress: v.PermanentAddress is not null
                ? new VerificationAddressDto(
                    FullAddress: v.PermanentAddress.FullAddress,
                    Province: v.PermanentAddress.Province,
                    District: v.PermanentAddress.District,
                    Ward: v.PermanentAddress.Ward)
                : null,
            Status: v.Status.Id,
            VerifiedAt: v.VerifiedAt,
            VerifiedBy: v.VerifiedBy?.Value,
            RejectionReason: v.RejectionReason,
            RejectionCode: v.RejectionCode,
            SubmittedAt: v.SubmittedAt,
            ExpiresAt: v.ExpiresAt,
            AttemptCount: v.AttemptCount,
            CreatedAt: v.CreatedAt,
            ModifiedAt: v.ModifiedAt,
            Documents: v.Documents.Select(d => d.ToDto()).ToList());
    }

    public static VerificationDocumentDto ToDto(this VerificationDocument d)
    {
        return new VerificationDocumentDto(
            Id: d.Id.Value,
            DocumentType: d.DocumentType.Id,
            ResourceType: d.ResourceType,
            SecureUrl: d.Info.SecureUrl,
            FileHash: d.FileHash,
            MimeType: d.MimeType,
            VerificationStatus: d.VerificationStatus.Id,
            UploadedAt: d.UploadedAt,
            CreatedAt: d.CreatedAt);
    }

    public static VerificationSummaryDto ToSummaryDto(this IdentityVerification v)
    {
        return new VerificationSummaryDto(
            Id: v.Id.Value,
            VerificationType: v.VerificationType.Id,
            FullName: v.FullName,
            Status: v.Status.Id,
            SubmittedAt: v.SubmittedAt,
            AttemptCount: v.AttemptCount,
            CreatedAt: v.CreatedAt);
    }
}
