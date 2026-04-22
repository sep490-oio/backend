using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetAllTermsDocuments;

internal sealed class GetAllTermsDocumentsQueryHandler : IQueryHandler<GetAllTermsDocumentsQuery, IReadOnlyList<TermsDocumentDto>>
{
    private readonly IDbContext _dbContext;

    public GetAllTermsDocumentsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<TermsDocumentDto>, Error>> Handle(GetAllTermsDocumentsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<TermsDocument>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var normalizedType = request.Type.Trim().ToLower();
            query = query.Where(x => x.TermType.ToLower() == normalizedType);
        }

        // IsActive filter maps directly to Status == Active (or != Active) — avoids
        // using the computed IsActive property inside EF Where (untranslatable).
        if (request.IsActive.HasValue)
        {
            query = request.IsActive.Value
                ? query.Where(x => x.Status == TermsDocumentStatus.Active)
                : query.Where(x => x.Status != TermsDocumentStatus.Active);
        }

        var entities = await query
            .OrderBy(x => x.TermType)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        var documents = entities
            .Select(x => x.ToDto())
            .ToList();

        return documents;
    }
}
