using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class SellerKycDocument
{
    public Guid Id { get; set; }

    public Guid KycId { get; set; }

    public string DocumentType { get; set; } = null!;

    public string FileUrl { get; set; } = null!;

    public string? FileName { get; set; }

    public int? FileSize { get; set; }

    public string? FileHash { get; set; }

    public string? MimeType { get; set; }

    public string? VerificationStatus { get; set; }

    public string? VerificationNotes { get; set; }

    public string? ExtractedData { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SellerKyc Kyc { get; set; } = null!;
}
