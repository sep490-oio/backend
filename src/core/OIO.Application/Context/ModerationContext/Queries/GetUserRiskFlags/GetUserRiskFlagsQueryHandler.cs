using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.ModerationContext.Mappings;

namespace OIO.Application.Context.ModerationContext.Queries.GetUserRiskFlags;

internal sealed class GetUserRiskFlagsQueryHandler
    : IQueryHandler<GetUserRiskFlagsQuery, IReadOnlyList<UserRiskFlagDto>>
{
    private readonly IDbContext _dbContext;

    public GetUserRiskFlagsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<UserRiskFlagDto>, Error>> Handle(
        GetUserRiskFlagsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = UserId.From(request.UserId);

        var flags = await _dbContext.Set<UserRiskFlag>()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.ToDto())
            .ToListAsync(cancellationToken);

        return flags;
    }
}
