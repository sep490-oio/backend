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
    : IQueryHandler<GetUserAddressesQuery, PagedList<UserAddressDto>>
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

    public async Task<Result<PagedList<UserAddressDto>, Error>> Handle(
        GetUserAddressesQuery request,
        CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return UserErrors.Auth.UserNotLoggedIn;

        var addresses = await _dbContext.Set<UserAddress>()
            .Where(a => a.UserId == _currentUser.UserId)
            .OrderByDescending(ua => ua.CreatedAt)
            .Page(request.PagedParameters)
            .Select(address => new UserAddressDto(
                Id: address.Id.Value,
                Type: address.Type.Id,
                RecipientName: address.RecipientName,
                PhoneNumber: address.PhoneNumber,
                Street: address.Address.Street,
                Ward: address.Address.Ward,
                District: address.Address.District,
                City: address.Address.City,
                PostalCode: address.Address.PostalCode,
                IsDefault: address.IsDefault))
            .ToListAsync(cancellationToken);
        
        var totalCount = await _dbContext.Set<UserAddress>()
            .Where(u => u.UserId == _currentUser.UserId)
            .CountAsync(cancellationToken);

        return  PagedList<UserAddressDto>.ToPagedList(
            addresses,
            totalCount, 
            request.PagedParameters);
    }
}