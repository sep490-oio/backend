using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Application.Context.UserContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Extensions;

namespace OIO.Application.Context.UserContext.Commands.AddAddress;

internal sealed class AddAddressCommandHandler
    : ICommandHandler<AddAddressCommand, UserAddressDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AddAddressCommandHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<UserAddressDto, Error>> Handle(
        AddAddressCommand request,
        CancellationToken cancellationToken)
    {
        var nowUtc =  _clock.UtcNow;

        var (_, _, address, addressError) = Address
            .Create(request.Street, request.Ward, request.District, request.City, request.PostalCode);
        
        var (_, _, phoneNumber, phoneNumberError) = PhoneNumber
            .Create(request.PhoneNumber, request.CountryCode);
        
        var result = Result.FirstError(addressError, phoneNumberError);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        var user = await _dbContext.GetByIdAsync<User, UserId>(
            id: _currentUser.UserId,
            queryBuilder: query => query
                .Include(x => x.Addresses),
            cancellationToken: cancellationToken);
        
        if (user is null)
            return UserErrors.User.NotFound(_currentUser.UserId);
        

        var addAddressResult = user.AddAddress(
            AddressType.FromId(request.Type).Value,
            request.RecipientName,
            phoneNumber,
            address,
            nowUtc,
            request.IsDefault);

        if (addAddressResult.IsFailure)
        {
            return addAddressResult.Error;
        }
        
        _dbContext.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return addAddressResult.Value.ToDto();
    }
}