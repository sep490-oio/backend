using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;

namespace OIO.Application.Context.UserContext.Services;

public sealed class VerificationDuplicateIdentityService
{
    private readonly IDbContext _dbContext;

    public VerificationDuplicateIdentityService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<VerificationDuplicateIdentityMatch?> FindDuplicateAsync(
        IdentityVerification verification,
        CancellationToken cancellationToken)
    {
        if (verification.Document is null)
            return null;

        var normalizedIdNumber = NormalizeIdNumber(verification.Document.IdNumber);
        if (string.IsNullOrWhiteSpace(normalizedIdNumber))
            return null;

        var candidates = await _dbContext.Set<IdentityVerification>()
            .AsNoTracking()
            .Where(v =>
                v.Id != verification.Id &&
                v.UserId != verification.UserId &&
                (v.Status == IdentityVerificationStatus.Submitted ||
                 v.Status == IdentityVerificationStatus.UnderReview ||
                 v.Status == IdentityVerificationStatus.Approved))
            .ToListAsync(cancellationToken);

        return candidates
            .Where(v => v.Document is not null)
            .Where(v => string.Equals(v.Document!.IdType.Id, verification.Document.IdType.Id, StringComparison.Ordinal))
            .Select(v => new VerificationDuplicateIdentityMatch(
                v.Id.Value,
                v.UserId.Value,
                v.Status.Id,
                v.Document!.IdType.Id,
                v.Document.IdNumber))
            .FirstOrDefault(v => string.Equals(NormalizeIdNumber(v.IdNumber), normalizedIdNumber, StringComparison.Ordinal));
    }

    public static string NormalizeIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return string.Empty;

        return new string(idNumber
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray())
            .ToUpperInvariant();
    }
}

public sealed record VerificationDuplicateIdentityMatch(
    Guid VerificationId,
    Guid UserId,
    string StatusId,
    string IdType,
    string IdNumber);
