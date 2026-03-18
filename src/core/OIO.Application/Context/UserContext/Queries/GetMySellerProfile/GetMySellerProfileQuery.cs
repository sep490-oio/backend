using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetMySellerProfile;

public sealed record GetMySellerProfileQuery : IQuery<SellerProfileDto>;

internal sealed class GetMySellerProfileQueryHandler
    : IQueryHandler<GetMySellerProfileQuery, SellerProfileDto>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMySellerProfileQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<SellerProfileDto, Error>> Handle(
        GetMySellerProfileQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;

        var profile = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, cancellationToken);

        if (profile is null)
            return UserErrors.SellerProfile.NotFound;

        return profile.ToDto();
    }
}
