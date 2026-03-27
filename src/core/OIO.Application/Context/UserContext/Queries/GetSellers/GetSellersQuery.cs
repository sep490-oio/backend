using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetSellers;

public record GetSellersFilterParameters : PagedParameters, ISortByParameter
{
    public string? Search { get; init; }
    public string? SortBy { get; init; }
}

public sealed record GetSellersQuery(GetSellersFilterParameters Parameters)
    : IQuery<PagedList<PublicSellerProfileDto>>;

internal sealed class GetSellersQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetSellersQuery, PagedList<PublicSellerProfileDto>>
{
    public async Task<Result<PagedList<PublicSellerProfileDto>, Error>> Handle(
        GetSellersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .Where(p => p.Status == SellerProfileStatus.Verified);

        // Search by store name
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(p => p.StoreName.ToLower().Contains(search));
        }

        query.ApplySort(parameters, SellerProfileMappings.PublicSellerProfileDtoSortMapping, p => p.TrustScoreOverall);
    
        var totalCount = await query.CountAsync(cancellationToken);
        
        var pagedResult = await query
            .Select(p => p.ToPublicDto())
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return pagedResult;
    }
}
