using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
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
        var acceptances = await _dbContext.Set<TermsAcceptance>()
            .AsNoTracking()
            .Include(x => x.TermDocument)
            .Where(x => x.UserId == _currentUser.UserId)
            .OrderByDescending(x => x.AcceptedAt)
            .Select(x => new TermsAcceptanceDto(
                x.Id.Value,
                x.AcceptedAt,
                x.IpAddress == null ? null : x.IpAddress.ToString(),
                x.UserAgent,
                new TermsDocumentDto(
                    x.TermDocument.Id.Value,
                    x.TermDocument.TermType,
                    x.TermDocument.Version,
                    x.TermDocument.IsActive,
                    x.TermDocument.PublishedAt,
                    x.TermDocument.CreatedAt,
                    x.TermDocument.Info.SecureUrl!,
                    x.TermDocument.Info.FileName,
                    x.TermDocument.Info.Bytes,
                    x.TermDocument.Info.Format,
                    x.TermDocument.Info.Width,
                    x.TermDocument.Info.Height,
                    x.TermDocument.Info.DurationSeconds,
                    x.TermDocument.StorageRef.PublicId,
                    x.TermDocument.StorageRef.Folder)))
            .ToListAsync(cancellationToken);

        return acceptances;
    }
}
