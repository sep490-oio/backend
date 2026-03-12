using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
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
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
            return TermsErrors.ActiveTermsByTypeNotFound(request.Type);

        return document;
    }
}
