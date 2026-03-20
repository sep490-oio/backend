using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetActiveTermsByType;

internal sealed class GetActiveTermsByTypeQueryHandler : IQueryHandler<GetActiveTermsByTypeQuery, TermsDocumentDto>
{
    private readonly IDbContext _dbContext;

    public GetActiveTermsByTypeQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<TermsDocumentDto, Error>> Handle(GetActiveTermsByTypeQuery request, CancellationToken cancellationToken)
    {
        var normalizedType = request.Type.Trim().ToLower();

        var document = await _dbContext.Set<TermsDocument>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.TermType.ToLower() == normalizedType)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
            return TermsErrors.ActiveTermsByTypeNotFound(request.Type);

        return document.ToDto();
    }
}
