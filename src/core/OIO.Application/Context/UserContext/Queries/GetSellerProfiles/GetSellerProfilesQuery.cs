using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetSellerProfiles;

public record GetSellerProfilesQueryFilter : PagedParameters;
public sealed record GetSellerProfilesQuery(GetSellerProfilesQueryFilter Parameters) : IQuery<PagedList<SellerProfileDto>>;

internal sealed class GetSellerProfilesQueryHandler
    : IQueryHandler<GetSellerProfilesQuery, PagedList<SellerProfileDto>>
{
    private readonly IDbContext _dbContext;

    public GetSellerProfilesQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<SellerProfileDto>, Error>> Handle(
        GetSellerProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        
        var query = _dbContext.Set<SellerProfile>()
            .OrderByDescending(p => p.CreatedAt);
        
        var totalCount = await query.CountAsync(cancellationToken);
           
        var profiles = await query
            .Select(x => x.ToDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return profiles;
    }
}
