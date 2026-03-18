using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetSellerProfiles;

public sealed record GetSellerProfilesQuery : IQuery<IReadOnlyCollection<SellerProfileDto>>;

internal sealed class GetSellerProfilesQueryHandler
    : IQueryHandler<GetSellerProfilesQuery, IReadOnlyCollection<SellerProfileDto>>
{
    private readonly IDbContext _dbContext;

    public GetSellerProfilesQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyCollection<SellerProfileDto>, Error>> Handle(
        GetSellerProfilesQuery request,
        CancellationToken cancellationToken)
    {
        var profiles = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtos = profiles.Select(p => p.ToDto()).ToList();

        return dtos;
    }
}
