using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Extensions;
using OIO.Application.UserContext.DTOs;
using OIO.Application.UserContext.Mappings;
using OIO.Application.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Queries.GetUserAddresses;

internal sealed class GetUserAddressesQueryHandler
    : IQueryHandler<GetUserAddressesQuery, PagedResult<UserAddressDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetUserAddressesQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<UserAddressDto>, Error>> Handle(
        GetUserAddressesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var addresses = await _dbContext.Set<UserAddress>()
            .Where(a => a.UserId == _currentUser.UserId)
            .OrderByDescending(ua => ua.CreatedAt)
            .Paged(request.PagedParameters)
            .Select(UserAddress.UserAddressProjectToDto())
            .ToListAsync(cancellationToken);
        
        var totalCount = await _dbContext.Set<UserAddress>()
            .Where(u => u.UserId == _currentUser.UserId)
            .CountAsync(cancellationToken);

        return  PagedResult<UserAddressDto>.ToPagedList(
            addresses,
            totalCount, 
            request.PagedParameters);
    }
}