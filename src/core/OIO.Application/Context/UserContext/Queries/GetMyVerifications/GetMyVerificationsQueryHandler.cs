using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetMyVerifications;

internal sealed class GetMyVerificationsQueryHandler
    : IQueryHandler<GetMyVerificationsQuery, IReadOnlyCollection<VerificationSummaryDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyVerificationsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyCollection<VerificationSummaryDto>, Error>> Handle(
        GetMyVerificationsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var verifications = await _dbContext.Set<IdentityVerification>()
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = verifications.Select(v => v.ToSummaryDto()).ToList();

        return dtos;
    }
}
