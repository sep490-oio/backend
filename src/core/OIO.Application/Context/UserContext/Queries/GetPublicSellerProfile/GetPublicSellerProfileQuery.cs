using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetPublicSellerProfile;

public sealed record GetPublicSellerProfileQuery(Guid SellerId) : IQuery<PublicSellerProfileDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetPublicSellerProfileQuery.Check()
            .WithOwnerName("GetPublicSellerProfile")
            .Field(SellerId).NotEmptyGuid();
    }
}

internal sealed class GetPublicSellerProfileQueryHandler
    : IQueryHandler<GetPublicSellerProfileQuery, PublicSellerProfileDto>
{
    private readonly IDbContext _dbContext;

    public GetPublicSellerProfileQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PublicSellerProfileDto, Error>> Handle(
        GetPublicSellerProfileQuery request,
        CancellationToken cancellationToken)
    {
        var sellerId = UserId.From(request.SellerId);

        var profile = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);

        if (profile is null)
            return UserErrors.SellerProfile.NotFoundById(sellerId);

        return profile.ToPublicDto();
    }
}
