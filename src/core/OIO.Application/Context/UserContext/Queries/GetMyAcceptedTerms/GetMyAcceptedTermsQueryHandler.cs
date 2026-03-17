using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetMyAcceptedTerms;

internal sealed class GetMyAcceptedTermsQueryHandler : IQueryHandler<GetMyAcceptedTermsQuery, IReadOnlyList<TermsAcceptanceDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyAcceptedTermsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<TermsAcceptanceDto>, Error>> Handle(GetMyAcceptedTermsQuery request, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Set<TermsAcceptance>()
            .AsNoTracking()
            .Include(x => x.TermDocument)
            .Where(x => x.UserId == _currentUser.UserId)
            .OrderByDescending(x => x.AcceptedAt)
            .ToListAsync(cancellationToken);

        var acceptances = entities
            .Select(x => x.ToDto())
            .ToList();

        return acceptances;
    }
}
