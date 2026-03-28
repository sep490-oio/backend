using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetPendingVerifications;

internal sealed class GetPendingVerificationsQueryHandler
    : IQueryHandler<GetPendingVerificationsQuery, PagedList<VerificationSummaryDto>>
{
    private readonly IDbContext _dbContext;

    public GetPendingVerificationsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<VerificationSummaryDto>, Error>> Handle(
        GetPendingVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        
        var verifications = _dbContext.Set<IdentityVerification>()
            .Where(v => v.Status == IdentityVerificationStatus.Submitted
                        || v.Status == IdentityVerificationStatus.UnderReview)
            .OrderBy(v => v.SubmittedAt);

        var totalCount = await verifications.CountAsync(cancellationToken);
        
        var dtos = await verifications
            .Select(v => v.ToSummaryDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return dtos;
    }
}
