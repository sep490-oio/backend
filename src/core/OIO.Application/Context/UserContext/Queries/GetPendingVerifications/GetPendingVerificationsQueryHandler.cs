using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetPendingVerifications;

internal sealed class GetPendingVerificationsQueryHandler
    : IQueryHandler<GetPendingVerificationsQuery, IReadOnlyCollection<VerificationSummaryDto>>
{
    private readonly IDbContext _dbContext;

    public GetPendingVerificationsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyCollection<VerificationSummaryDto>, Error>> Handle(
        GetPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var verifications = await _dbContext.Set<IdentityVerification>()
            .AsNoTracking()
            .Where(v => v.Status == IdentityVerificationStatus.Submitted
                        || v.Status == IdentityVerificationStatus.UnderReview)
            .OrderBy(v => v.SubmittedAt)
            .ToListAsync(cancellationToken);

        var dtos = verifications.Select(v => v.ToSummaryDto()).ToList();

        return dtos;
    }
}
