using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetActiveTerms;

internal sealed class GetActiveTermsQueryHandler : IQueryHandler<GetActiveTermsQuery, IReadOnlyList<TermsDocumentDto>>
{
    private readonly IDbContext _dbContext;

    public GetActiveTermsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TermsDocumentDto>, Error>> Handle(GetActiveTermsQuery request, CancellationToken cancellationToken)
    {
        var entities = await _dbContext.Set<TermsDocument>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.TermType)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        var documents = entities
            .Select(x => x.ToDto())
            .ToList();

        return documents;
    }
}
