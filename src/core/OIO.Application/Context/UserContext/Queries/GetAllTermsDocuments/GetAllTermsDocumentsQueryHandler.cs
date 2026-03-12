using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;
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

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        var documents = await query
            .OrderBy(x => x.TermType)
            .ThenByDescending(x => x.Version)
            .Select(x => new TermsDocumentDto(
                x.Id.Value,
                x.TermType,
                x.Version,
                x.IsActive,
                x.PublishedAt,
                x.CreatedAt,
                x.Info.SecureUrl!,
                x.Info.FileName,
                x.Info.Bytes,
                x.Info.Format,
                x.Info.Width,
                x.Info.Height,
                x.Info.DurationSeconds,
                x.StorageRef.PublicId,
                x.StorageRef.Folder))
            .ToListAsync(cancellationToken);

        return documents;
    }
}
