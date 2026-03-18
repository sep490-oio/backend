using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetUserAddresses;

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
        

        var addresses = await _dbContext.Set<UserAddress>()
            .Where(a => a.UserId == _currentUser.UserId)
            .OrderByDescending(ua => ua.CreatedAt)
            .Page(request.PagedParameters)
            .Select(address => new UserAddressDto(
                Id: address.Id.Value,
                Type: address.Type.Id,
                RecipientName: address.Recipient.RecipientName,
                PhoneNumber: address.Recipient.Phone,
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