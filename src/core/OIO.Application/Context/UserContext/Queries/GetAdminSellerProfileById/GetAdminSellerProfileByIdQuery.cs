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

namespace OIO.Application.Context.UserContext.Queries.GetAdminSellerProfileById;

public sealed record GetAdminSellerProfileByIdQuery(Guid SellerId) : IQuery<AdminSellerProfileDetailDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAdminSellerProfileByIdQuery.Check()
            .WithOwnerName("GetAdminSellerProfileById")
            .Field(SellerId).NotEmptyGuid();
    }
}

internal sealed class GetAdminSellerProfileByIdQueryHandler
    : IQueryHandler<GetAdminSellerProfileByIdQuery, AdminSellerProfileDetailDto>
{
    private readonly IDbContext _dbContext;

    public GetAdminSellerProfileByIdQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminSellerProfileDetailDto, Error>> Handle(
        GetAdminSellerProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var sellerId = UserId.From(request.SellerId);

        var profile = await _dbContext.Set<SellerProfile>()
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == sellerId, cancellationToken);
            
        if (profile is null)
            return UserErrors.SellerProfile.NotFoundById(sellerId);
            
        return new AdminSellerProfileDetailDto(
            Profile: profile.ToDto(),
            User: profile.User.ToDto()
        );
    }
}
